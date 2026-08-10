using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Notifications.DTOs;

public sealed record NotificationDto(Guid Id, Guid UserId, NotificationType Type, string Title, string Message, string? RelatedEntityType, Guid? RelatedEntityId, bool IsRead, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);
public sealed record NotificationPreferenceDto(Guid Id, Guid UserId, NotificationPreferenceType PreferenceType, NotificationChannel Channel, bool IsEnabled);
