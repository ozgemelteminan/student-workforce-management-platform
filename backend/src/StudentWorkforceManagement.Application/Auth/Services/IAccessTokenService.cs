using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Application.Auth.Services;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IAccessTokenService
{
    AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> roles, Guid sessionId);
}
