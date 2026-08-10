using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Auth.DTOs;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Email;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Auth.Commands;

public sealed record InviteUserCommand(string Email, DateTimeOffset ExpiresAt) : IRequest<CreatedInvitationDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record InviteStudentCommand(string Email, DateTimeOffset ExpiresAt) : IRequest<CreatedInvitationDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record ResendInvitationCommand(Guid InvitationId, DateTimeOffset ExpiresAt) : IRequest<CreatedInvitationDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record RevokeInvitationCommand(Guid InvitationId) : IRequest<InvitationDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record AcceptInvitationCommand(string RawToken, string Password, string DisplayName, string? FirstName = null, string? LastName = null, string? Department = null) : IRequest<InvitationDto>, ITransactionalRequest;

public sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.ExpiresAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Expiration must be provided as a UTC DateTimeOffset.");
    }
}

public sealed class InviteStudentCommandValidator : AbstractValidator<InviteStudentCommand>
{
    public InviteStudentCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.ExpiresAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Expiration must be provided as a UTC DateTimeOffset.");
    }
}

public sealed class ResendInvitationCommandValidator : AbstractValidator<ResendInvitationCommand>
{
    public ResendInvitationCommandValidator()
    {
        RuleFor(command => command.InvitationId).NotEmpty();
        RuleFor(command => command.ExpiresAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Expiration must be provided as a UTC DateTimeOffset.");
    }
}

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(command => command.RawToken).NotEmpty().MaximumLength(2048);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
        RuleFor(command => command.Password).Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.");
        RuleFor(command => command.Password).Matches("[a-z]").WithMessage("Password must contain a lowercase letter.");
        RuleFor(command => command.Password).Matches("[0-9]").WithMessage("Password must contain a number.");
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.FirstName).MaximumLength(120);
        RuleFor(command => command.LastName).MaximumLength(120);
        RuleFor(command => command.Department).MaximumLength(160);
    }
}

public sealed class InvitationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    ISecureTokenGenerator tokenGenerator,
    IUtcClock clock,
    IEmailService emailService,
    IAuditService auditService,
    IPasswordService passwordService)
    : IRequestHandler<InviteUserCommand, CreatedInvitationDto>,
      IRequestHandler<InviteStudentCommand, CreatedInvitationDto>,
      IRequestHandler<ResendInvitationCommand, CreatedInvitationDto>,
      IRequestHandler<RevokeInvitationCommand, InvitationDto>,
      IRequestHandler<AcceptInvitationCommand, InvitationDto>
{
    public System.Threading.Tasks.Task<CreatedInvitationDto> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        return CreateInvitationAsync(request.Email, request.ExpiresAt, "auth.invitation.user", cancellationToken);
    }

    public System.Threading.Tasks.Task<CreatedInvitationDto> Handle(InviteStudentCommand request, CancellationToken cancellationToken)
    {
        return CreateInvitationAsync(request.Email, request.ExpiresAt, "auth.invitation.student", cancellationToken);
    }

    public async System.Threading.Tasks.Task<CreatedInvitationDto> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await dbContext.Invitations.SingleOrDefaultAsync(entity => entity.Id == request.InvitationId, cancellationToken)
            ?? throw new NotFoundException("Invitation", request.InvitationId);
        if (invitation.AcceptedAt.HasValue)
        {
            throw new ConflictException("Accepted invitations cannot be resent.");
        }
        if (invitation.RevokedAt.HasValue)
        {
            throw new ConflictException("Revoked invitations cannot be resent.");
        }

        var rawToken = tokenGenerator.GenerateToken();
        invitation.TokenHash = tokenGenerator.HashToken(rawToken);
        invitation.ExpiresAt = request.ExpiresAt.ToUniversalTime();
        await QueueInvitationEmailAsync(invitation.Email, rawToken, "auth.invitation.resend", invitation.Id, cancellationToken);
        await auditService.RecordAsync("InvitationResent", "Invitation", invitation.Id, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CreatedInvitationDto(invitation.Id, invitation.Email, invitation.ExpiresAt, rawToken);
    }

    public async System.Threading.Tasks.Task<InvitationDto> Handle(RevokeInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await dbContext.Invitations.SingleOrDefaultAsync(entity => entity.Id == request.InvitationId, cancellationToken)
            ?? throw new NotFoundException("Invitation", request.InvitationId);
        if (!invitation.AcceptedAt.HasValue && !invitation.RevokedAt.HasValue)
        {
            invitation.RevokedAt = clock.UtcNow;
            await auditService.RecordAsync("InvitationRevoked", "Invitation", invitation.Id, cancellationToken: cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return ToDto(invitation);
    }

    public async System.Threading.Tasks.Task<InvitationDto> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenGenerator.HashToken(request.RawToken);
        var invitation = await dbContext.Invitations.SingleOrDefaultAsync(entity => entity.TokenHash == tokenHash, cancellationToken)
            ?? throw new NotFoundException("Invitation", "token");
        if (invitation.RevokedAt.HasValue)
        {
            throw new ConflictException("Invitation has been revoked.");
        }
        if (invitation.AcceptedAt.HasValue)
        {
            throw new ConflictException("Invitation has already been accepted.");
        }
        if (invitation.ExpiresAt <= clock.UtcNow)
        {
            throw new ConflictException("Invitation has expired.");
        }

        var user = await dbContext.Users.Include(entity => entity.Student).SingleOrDefaultAsync(entity => entity.Email == invitation.Email, cancellationToken);
        if (user is null)
        {
            var studentRole = await dbContext.Roles.SingleOrDefaultAsync(role => role.Name == UserRole.STUDENT, cancellationToken)
                ?? throw new ConflictException("Student role is not configured.");
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = invitation.Email,
                DisplayName = request.DisplayName.Trim(),
                RoleId = studentRole.Id,
                IsActive = true
            };
            dbContext.Users.Add(user);
        }

        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = true;
        user.PasswordHash = passwordService.HashPassword(user, request.Password);

        if (!string.IsNullOrWhiteSpace(request.FirstName) && !string.IsNullOrWhiteSpace(request.LastName) && !string.IsNullOrWhiteSpace(request.Department) && user.Student is null)
        {
            dbContext.Students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = invitation.Email,
                Department = request.Department.Trim(),
                IsActive = true
            });
        }

        invitation.AcceptedAt = clock.UtcNow;
        await auditService.RecordAsync("InvitationAccepted", "Invitation", invitation.Id, cancellationToken: cancellationToken);
        return ToDto(invitation);
    }

    private async System.Threading.Tasks.Task<CreatedInvitationDto> CreateInvitationAsync(string email, DateTimeOffset expiresAt, string templateKey, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (expiresAt <= clock.UtcNow)
        {
            throw new ConflictException("Invitation expiration must be in the future.");
        }
        var rawToken = tokenGenerator.GenerateToken();
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            CreatedById = currentUser.RequireUserId(),
            TokenHash = tokenGenerator.HashToken(rawToken),
            ExpiresAt = expiresAt.ToUniversalTime()
        };
        dbContext.Invitations.Add(invitation);
        await QueueInvitationEmailAsync(normalizedEmail, rawToken, templateKey, invitation.Id, cancellationToken);
        await auditService.RecordAsync("InvitationCreated", "Invitation", invitation.Id, newValue: normalizedEmail, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CreatedInvitationDto(invitation.Id, invitation.Email, invitation.ExpiresAt, rawToken);
    }

    private System.Threading.Tasks.Task QueueInvitationEmailAsync(string email, string rawToken, string templateKey, Guid invitationId, CancellationToken cancellationToken)
    {
        return emailService.QueueAsync(new EmailMessage(
            email,
            "Your Student Workforce invitation",
            templateKey,
            new Dictionary<string, string> { ["invitationToken"] = rawToken, ["invitationId"] = invitationId.ToString("N") },
            $"invitation:{invitationId:N}"), cancellationToken);
    }

    private static InvitationDto ToDto(Invitation invitation)
    {
        return new InvitationDto(invitation.Id, invitation.Email, invitation.ExpiresAt, invitation.AcceptedAt, invitation.RevokedAt, invitation.CreatedById, invitation.CreatedAt);
    }
}
