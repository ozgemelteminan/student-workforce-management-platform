using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Marketplace.DTOs;

public sealed record MarketplaceListingDto(Guid Id, Guid TaskId, MarketplaceListingStatus Status, MarketplaceApprovalMode ApprovalMode, DateTimeOffset? PublishedAt, DateTimeOffset? ExpiresAt, Guid ConcurrencyToken);
public sealed record MarketplaceClaimDto(Guid Id, Guid MarketplaceListingId, Guid StudentId, MarketplaceClaimStatus Status, DateTimeOffset ClaimedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? ApprovedAt, DateTimeOffset? RejectedAt, Guid ConcurrencyToken);
