using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Common.Security;

public sealed class AnonymousCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? StudentId => null;
    public bool IsAuthenticated => false;
    public IReadOnlyCollection<UserRole> Roles => Array.Empty<UserRole>();
}
