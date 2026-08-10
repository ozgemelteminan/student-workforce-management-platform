using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class MarketplaceListing : AuditableEntity, IHasConcurrencyToken
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public MarketplaceListingStatus Status { get; set; }
    public MarketplaceApprovalMode ApprovalMode { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? UnpublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid? PublishedById { get; set; }
    public User? PublishedBy { get; set; }
    public Guid ConcurrencyToken { get; set; }

    public ICollection<MarketplaceClaim> Claims { get; set; } = new List<MarketplaceClaim>();
}
