using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class User : AuditableEntity, ISoftDeletable, IHasConcurrencyToken
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? PasswordHash { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public Student? Student { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }

    public ICollection<Invitation> InvitationsCreated { get; set; } = new List<Invitation>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
