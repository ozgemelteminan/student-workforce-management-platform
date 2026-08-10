using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class MarketplaceListing : Entity, IHasConcurrencyToken
{
    public Guid TaskId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
