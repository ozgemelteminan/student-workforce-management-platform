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

namespace StudentWorkforceManagement.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password, string? DeviceName, string? IpAddress, DateTimeOffset SessionExpiresAt, DateTimeOffset RefreshTokenExpiresAt) : IRequest<AuthenticationResultDto>;

public sealed record ForgotPasswordCommand(string Email, DateTimeOffset ExpiresAt) : IRequest<PasswordResetRequestDto>;

public sealed record ResetPasswordCommand(string RawResetToken, string NewPassword) : IRequest<PasswordResetResultDto>, ITransactionalRequest;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(1024);
        RuleFor(command => command.DeviceName).MaximumLength(200);
        RuleFor(command => command.IpAddress).MaximumLength(64);
        RuleFor(command => command.SessionExpiresAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Session expiration must be UTC.");
        RuleFor(command => command.RefreshTokenExpiresAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Refresh token expiration must be UTC.");
    }
}

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.ExpiresAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Reset token expiration must be UTC.");
    }
}

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.RawResetToken).NotEmpty().MaximumLength(2048);
        RuleFor(command => command.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(256);
        RuleFor(command => command.NewPassword).Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.");
        RuleFor(command => command.NewPassword).Matches("[a-z]").WithMessage("Password must contain a lowercase letter.");
        RuleFor(command => command.NewPassword).Matches("[0-9]").WithMessage("Password must contain a number.");
    }
}

public sealed class PasswordAuthContractHandler(
    IApplicationDbContext dbContext,
    ISecureTokenGenerator tokenGenerator,
    IPasswordService passwordService,
    IAccessTokenService accessTokenService,
    IUtcClock clock,
    IEmailService emailService,
    IAuditService auditService)
    : IRequestHandler<LoginCommand, AuthenticationResultDto>, IRequestHandler<ForgotPasswordCommand, PasswordResetRequestDto>, IRequestHandler<ResetPasswordCommand, PasswordResetResultDto>
{
    public async System.Threading.Tasks.Task<AuthenticationResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users.Include(entity => entity.Role).Include(entity => entity.Student).SingleOrDefaultAsync(entity => entity.Email == normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive || user.DeletedAt.HasValue || string.IsNullOrWhiteSpace(user.PasswordHash) || !passwordService.VerifyPassword(user, request.Password))
        {
            throw new ForbiddenException("Invalid email or password.");
        }
        if (request.SessionExpiresAt <= clock.UtcNow || request.RefreshTokenExpiresAt <= clock.UtcNow)
        {
            throw new ConflictException("Authentication token expiration must be in the future.");
        }

        var rawRefreshToken = tokenGenerator.GenerateToken();
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceName = request.DeviceName,
            IpAddress = request.IpAddress,
            ExpiresAt = request.SessionExpiresAt.ToUniversalTime()
        };
        dbContext.Sessions.Add(session);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = tokenGenerator.HashToken(rawRefreshToken),
            ExpiresAt = request.RefreshTokenExpiresAt.ToUniversalTime()
        });
        await auditService.RecordAsync("UserLoggedIn", "User", user.Id, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var roles = user.Role is null ? Array.Empty<string>() : new[] { user.Role.Name.ToString() };
        var accessToken = accessTokenService.CreateAccessToken(user, roles, session.Id);
        return new AuthenticationResultDto(user.Id, session.Id, user.Email, user.DisplayName, roles, accessToken.Token, accessToken.ExpiresAt, rawRefreshToken, session.ExpiresAt, request.RefreshTokenExpiresAt.ToUniversalTime());
    }

    public async System.Threading.Tasks.Task<PasswordResetRequestDto> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (request.ExpiresAt <= clock.UtcNow)
        {
            throw new ConflictException("Reset token expiration must be in the future.");
        }
        var user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Email == normalizedEmail && entity.IsActive && entity.DeletedAt == null, cancellationToken);
        if (user is null)
        {
            return new PasswordResetRequestDto(normalizedEmail, request.ExpiresAt.ToUniversalTime());
        }

        foreach (var token in await dbContext.PasswordResetTokens.Where(token => token.UserId == user.Id && token.ConsumedAt == null && token.RevokedAt == null && token.ExpiresAt > clock.UtcNow).ToListAsync(cancellationToken))
        {
            token.RevokedAt = clock.UtcNow;
        }
        var rawToken = tokenGenerator.GenerateToken();
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenGenerator.HashToken(rawToken),
            ExpiresAt = request.ExpiresAt.ToUniversalTime()
        };
        dbContext.PasswordResetTokens.Add(resetToken);
        await emailService.QueueAsync(new EmailMessage(
            normalizedEmail,
            "Reset your Student Workforce password",
            "auth.password-reset",
            new Dictionary<string, string> { ["userId"] = user.Id.ToString("N") },
            $"password-reset:{resetToken.Id:N}",
            new Dictionary<string, string> { ["resetToken"] = rawToken }), cancellationToken);
        await auditService.RecordAsync("PasswordResetRequested", "User", user.Id, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new PasswordResetRequestDto(normalizedEmail, resetToken.ExpiresAt);
    }

    public async System.Threading.Tasks.Task<PasswordResetResultDto> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenGenerator.HashToken(request.RawResetToken);
        var token = await dbContext.PasswordResetTokens.Include(entity => entity.User).SingleOrDefaultAsync(entity => entity.TokenHash == tokenHash, cancellationToken)
            ?? throw new NotFoundException("PasswordResetToken", "token");
        if (token.User is null || !token.User.IsActive || token.User.DeletedAt.HasValue)
        {
            throw new ConflictException("Password reset token is not valid.");
        }
        if (token.ConsumedAt.HasValue || token.RevokedAt.HasValue || token.ExpiresAt <= clock.UtcNow)
        {
            throw new ConflictException("Password reset token is not active.");
        }

        token.User.PasswordHash = passwordService.HashPassword(token.User, request.NewPassword);
        token.ConsumedAt = clock.UtcNow;
        foreach (var otherToken in await dbContext.PasswordResetTokens.Where(other => other.UserId == token.UserId && other.Id != token.Id && other.ConsumedAt == null && other.RevokedAt == null).ToListAsync(cancellationToken))
        {
            otherToken.RevokedAt = clock.UtcNow;
        }
        var activeSessions = await dbContext.Sessions.Where(session => session.UserId == token.UserId && session.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var session in activeSessions)
        {
            session.RevokedAt = clock.UtcNow;
        }
        var sessionIds = activeSessions.Select(session => session.Id).ToArray();
        var activeRefreshTokens = await dbContext.RefreshTokens.Where(refresh => sessionIds.Contains(refresh.SessionId) && refresh.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAt = clock.UtcNow;
        }
        await auditService.RecordAsync("PasswordChanged", "User", token.UserId, cancellationToken: cancellationToken);
        return new PasswordResetResultDto(token.UserId, token.ConsumedAt.Value, activeSessions.Count);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
