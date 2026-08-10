using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class NotificationPreference : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public NotificationPreferenceType PreferenceType { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool IsEnabled { get; set; } = true;
}
