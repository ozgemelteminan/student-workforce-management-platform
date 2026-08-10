using StudentWorkforceManagement.Domain.Common;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class PasswordResetToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
