using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Notifications.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Notifications.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery
    : IRequest<IReadOnlyCollection<NotificationPreferenceSettingDto>>,
      IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetNotificationPreferencesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IRequestHandler<
        GetNotificationPreferencesQuery,
        IReadOnlyCollection<NotificationPreferenceSettingDto>>
{
    public async Task<IReadOnlyCollection<NotificationPreferenceSettingDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        return await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NotificationPreferenceSettingDto(
                x.PreferenceType,
                x.Channel,
                x.IsEnabled))
            .OrderBy(preference => preference.PreferenceType)
            .ThenBy(preference => preference.Channel)
            .ToListAsync(cancellationToken);
    }
}
