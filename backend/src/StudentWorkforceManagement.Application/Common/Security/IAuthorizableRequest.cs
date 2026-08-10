using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Common.Security;

public interface IAuthorizableRequest
{
    IReadOnlyCollection<UserRole> RequiredRoles { get; }
}
