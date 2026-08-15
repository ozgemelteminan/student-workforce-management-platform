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
        var persisted = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.PreferenceType,
                x.Channel,
                x.IsEnabled
            })
            .ToListAsync(cancellationToken);

        var byKey = persisted.ToDictionary(
            preference => (preference.PreferenceType, preference.Channel),
            preference => preference.IsEnabled);

        return Enum.GetValues<NotificationPreferenceType>()
            .SelectMany(preferenceType => Enum.GetValues<NotificationChannel>()
                .Select(channel => new NotificationPreferenceSettingDto(
                    preferenceType,
                    channel,
                    byKey.TryGetValue((preferenceType, channel), out var isEnabled) ? isEnabled : true)))
            .OrderBy(preference => preference.PreferenceType)
            .ThenBy(preference => preference.Channel)
            .ToList();
    }
}
