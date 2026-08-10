using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class MarketplaceClaim : AuditableEntity, IHasConcurrencyToken
{
    public Guid MarketplaceListingId { get; set; }
    public MarketplaceListing? MarketplaceListing { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public MarketplaceClaimStatus Status { get; set; }
    public DateTimeOffset ClaimedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public Guid? RejectedById { get; set; }
    public User? RejectedBy { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
