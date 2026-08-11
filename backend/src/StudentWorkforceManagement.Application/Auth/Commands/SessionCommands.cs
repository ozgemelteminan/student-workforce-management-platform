using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Auth.DTOs;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Auth.Commands;

public sealed record CreateSessionCommand(Guid UserId, string? DeviceName, string? IpAddress, DateTimeOffset ExpiresAt) : IRequest<SessionDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record RefreshTokenCommand(string RawRefreshToken, DateTimeOffset NewExpiresAt) : IRequest<AuthenticationResultDto>, ITransactionalRequest;

public sealed record LogoutCommand(Guid SessionId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record RevokeSessionCommand(Guid SessionId) : IRequest<SessionDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record RevokeAllSessionsCommand(Guid? UserId = null) : IRequest<int>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.DeviceName).MaximumLength(200);
        RuleFor(command => command.IpAddress).MaximumLength(64);
        RuleFor(command => command.ExpiresAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Session expiration must be UTC.");
    }
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RawRefreshToken).NotEmpty().MaximumLength(2048);
        RuleFor(command => command.NewExpiresAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Refresh token expiration must be UTC.");
    }
}

public sealed class SessionCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, ISecureTokenGenerator tokenGenerator, IAccessTokenService accessTokenService, IUtcClock clock)
    : IRequestHandler<CreateSessionCommand, SessionDto>,
      IRequestHandler<RefreshTokenCommand, AuthenticationResultDto>,
      IRequestHandler<LogoutCommand, Unit>,
      IRequestHandler<RevokeSessionCommand, SessionDto>,
      IRequestHandler<RevokeAllSessionsCommand, int>
{
    public async System.Threading.Tasks.Task<SessionDto> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == request.UserId && user.IsActive, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("User", request.UserId);
        }
        if (request.ExpiresAt <= clock.UtcNow)
        {
            throw new ConflictException("Session expiration must be in the future.");
        }
        var session = new Session { Id = Guid.NewGuid(), UserId = request.UserId, DeviceName = request.DeviceName, IpAddress = request.IpAddress, ExpiresAt = request.ExpiresAt.ToUniversalTime() };
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(session);
    }

    public async System.Threading.Tasks.Task<AuthenticationResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var oldHash = tokenGenerator.HashToken(request.RawRefreshToken);
        var existing = await dbContext.RefreshTokens
            .Include(token => token.Session)
                .ThenInclude(session => session!.User)
                    .ThenInclude(user => user!.Role)
            .Include(token => token.Session)
                .ThenInclude(session => session!.User)
                    .ThenInclude(user => user!.Student)
            .SingleOrDefaultAsync(token => token.TokenHash == oldHash, cancellationToken);
        if (existing is null)
        {
            throw new ForbiddenException("Refresh token is not valid.");
        }

        var session = existing.Session;
        var user = session?.User;
        if (existing.RevokedAt.HasValue
            || existing.ReplacedAt.HasValue
            || existing.ExpiresAt <= clock.UtcNow
            || session is null
            || session.RevokedAt.HasValue
            || session.ExpiresAt <= clock.UtcNow
            || user is null
            || !user.IsActive
            || user.DeletedAt.HasValue)
        {
            throw new ForbiddenException("Refresh token is not valid.");
        }

        var rawNewToken = tokenGenerator.GenerateToken();
        existing.ReplacedAt = clock.UtcNow;
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = existing.SessionId,
            TokenHash = tokenGenerator.HashToken(rawNewToken),
            ExpiresAt = request.NewExpiresAt.ToUniversalTime()
        });
        var roles = user.Role is null ? Array.Empty<string>() : new[] { user.Role.Name.ToString() };
        var accessToken = accessTokenService.CreateAccessToken(user, roles, session.Id);
        return new AuthenticationResultDto(user.Id, session.Id, user.Email, user.DisplayName, roles, accessToken.Token, accessToken.ExpiresAt, rawNewToken, session.ExpiresAt, request.NewExpiresAt.ToUniversalTime());
    }

    public async System.Threading.Tasks.Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var session = await LoadAuthorizedSessionAsync(request.SessionId, cancellationToken);
        if (!session.RevokedAt.HasValue)
        {
            session.RevokedAt = clock.UtcNow;
            foreach (var token in await dbContext.RefreshTokens.Where(token => token.SessionId == session.Id && token.RevokedAt == null).ToListAsync(cancellationToken))
            {
                token.RevokedAt = clock.UtcNow;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return Unit.Value;
    }

    public async System.Threading.Tasks.Task<SessionDto> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await LoadAuthorizedSessionAsync(request.SessionId, cancellationToken);
        session.RevokedAt ??= clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(session);
    }

    public async System.Threading.Tasks.Task<int> Handle(RevokeAllSessionsCommand request, CancellationToken cancellationToken)
    {
        var targetUserId = request.UserId ?? currentUser.RequireUserId();
        if (!currentUser.IsInRole(UserRole.ADMIN) && targetUserId != currentUser.UserId)
        {
            throw new ForbiddenException("Users may revoke only their own sessions.");
        }
        var sessions = await dbContext.Sessions.Where(session => session.UserId == targetUserId && session.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = clock.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return sessions.Count;
    }

    private async System.Threading.Tasks.Task<Session> LoadAuthorizedSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.SingleOrDefaultAsync(entity => entity.Id == sessionId, cancellationToken)
            ?? throw new NotFoundException("Session", sessionId);
        if (!currentUser.IsInRole(UserRole.ADMIN) && session.UserId != currentUser.UserId)
        {
            throw new ForbiddenException("Users may revoke only their own sessions.");
        }
        return session;
    }

    private static SessionDto ToDto(Session session) => new(session.Id, session.UserId, session.DeviceName, session.IpAddress, session.ExpiresAt, session.RevokedAt, session.CreatedAt);
}
