using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Application.Auth.Services;

public interface IAccessTokenService
{
    string CreateAccessToken(User user, IReadOnlyCollection<string> roles, Guid sessionId);
}
