using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Common.Security;

public static class Authorize
{
    public static readonly IReadOnlyCollection<UserRole> Authenticated = Array.Empty<UserRole>();
    public static readonly IReadOnlyCollection<UserRole> AdminOnly = new[] { UserRole.ADMIN };
    public static readonly IReadOnlyCollection<UserRole> StaffTaskManagement = new[] { UserRole.ADMIN, UserRole.TASK_MANAGER };
    public static readonly IReadOnlyCollection<UserRole> Reviewers = new[] { UserRole.ADMIN, UserRole.REVIEWER };
    public static readonly IReadOnlyCollection<UserRole> Students = new[] { UserRole.STUDENT };
    public static readonly IReadOnlyCollection<UserRole> AnyRole = new[] { UserRole.ADMIN, UserRole.TASK_MANAGER, UserRole.REVIEWER, UserRole.STUDENT };
}
