using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Notifications.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery : PagedQuery, IRequest<PaginatedResult<NotificationDto>>, IAuthorizableRequest
{
    public bool? IsRead { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetUnreadNotificationCountQuery : IRequest<int>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class GetNotificationsQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationsQuery, PaginatedResult<NotificationDto>>, IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public async System.Threading.Tasks.Task<PaginatedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var query = dbContext.Notifications.AsNoTracking().Where(notification => notification.UserId == userId);
        if (request.IsRead.HasValue) query = query.Where(notification => notification.IsRead == request.IsRead.Value);
        return await query.OrderByDescending(notification => notification.CreatedAt).Select(notification => new NotificationDto(notification.Id, notification.UserId, notification.Type, notification.Title, notification.Message, notification.RelatedEntityType, notification.RelatedEntityId, notification.IsRead, notification.CreatedAt, notification.ReadAt)).ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public System.Threading.Tasks.Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        return dbContext.Notifications.AsNoTracking().CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken);
    }
}
