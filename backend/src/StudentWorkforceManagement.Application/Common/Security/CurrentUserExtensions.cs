using StudentWorkforceManagement.Application.Common.Exceptions;

namespace StudentWorkforceManagement.Application.Common.Security;

public static class CurrentUserExtensions
{
    public static Guid RequireUserId(this ICurrentUserService currentUser)
    {
        return currentUser.UserId ?? throw new ForbiddenException("Authenticated user id is required.");
    }

    public static Guid RequireStudentId(this ICurrentUserService currentUser)
    {
        return currentUser.StudentId ?? throw new ForbiddenException("Authenticated student id is required.");
    }
}
