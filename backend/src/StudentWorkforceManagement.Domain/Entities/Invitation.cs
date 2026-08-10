using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Invitation : Entity
{
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
