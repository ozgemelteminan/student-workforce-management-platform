using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Common.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? StudentId { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<UserRole> Roles { get; }

    bool IsInRole(UserRole role) => Roles.Contains(role);
}
