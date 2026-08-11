using System.Security.Claims;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Api.Authentication;

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => TryGetGuid(ClaimTypes.NameIdentifier) ?? TryGetGuid("sub");

    public Guid? StudentId => TryGetGuid("student_id");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true && UserId.HasValue;

    public IReadOnlyCollection<UserRole> Roles => User?.Claims
        .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == "role")
        .Select(claim => Enum.TryParse<UserRole>(claim.Value, ignoreCase: true, out var role) ? role : (UserRole?)null)
        .Where(role => role.HasValue)
        .Select(role => role!.Value)
        .Distinct()
        .ToArray() ?? Array.Empty<UserRole>();

    private Guid? TryGetGuid(string claimType)
    {
        var value = User?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
