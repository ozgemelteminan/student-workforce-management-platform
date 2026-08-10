using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class NotificationPreference : Entity
{
    public Guid UserId { get; set; }
    public NotificationPreferenceType PreferenceType { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool IsEnabled { get; set; }
}
