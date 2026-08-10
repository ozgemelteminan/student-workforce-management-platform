using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class User : Entity, ISoftDeletable, IHasConcurrencyToken
{
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
