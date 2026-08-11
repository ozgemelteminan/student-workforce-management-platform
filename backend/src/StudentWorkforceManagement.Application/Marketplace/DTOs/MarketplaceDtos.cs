using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Marketplace.DTOs;

public sealed record MarketplaceRequiredSkillSummaryDto(Guid SkillId, string SkillName, SkillLevel MinimumLevel);
public sealed record MarketplaceTaskSummaryDto(Guid TaskId, string Title, string? Description, Guid CategoryId, string? CategoryName, TaskPriority Priority, DateTimeOffset Deadline, int EstimatedDurationMinutes, IReadOnlyCollection<MarketplaceRequiredSkillSummaryDto> RequiredSkills);
public sealed record MarketplaceListingDto(Guid Id, Guid TaskId, MarketplaceListingStatus Status, MarketplaceApprovalMode ApprovalMode, DateTimeOffset? PublishedAt, DateTimeOffset? ExpiresAt, Guid ConcurrencyToken, MarketplaceTaskSummaryDto? TaskSummary);
public sealed record MarketplaceClaimDto(Guid Id, Guid MarketplaceListingId, Guid StudentId, MarketplaceClaimStatus Status, DateTimeOffset ClaimedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? ApprovedAt, DateTimeOffset? RejectedAt, Guid ConcurrencyToken);
