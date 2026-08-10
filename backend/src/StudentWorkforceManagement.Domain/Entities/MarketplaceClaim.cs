using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class MarketplaceClaim : Entity, IHasConcurrencyToken
{
    public Guid MarketplaceListingId { get; set; }
    public Guid StudentId { get; set; }
    public MarketplaceClaimStatus Status { get; set; }
    public DateTimeOffset ClaimedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
