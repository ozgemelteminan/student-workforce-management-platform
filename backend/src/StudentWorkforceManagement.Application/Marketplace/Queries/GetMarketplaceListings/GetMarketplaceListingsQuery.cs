using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Marketplace.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Marketplace.Queries.GetMarketplaceListings;

public sealed record GetMarketplaceListingsQuery : PagedQuery, IRequest<PaginatedResult<MarketplaceListingDto>>, IAuthorizableRequest
{
    public MarketplaceListingStatus? Status { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetMarketplaceListingsQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext)
    : IRequestHandler<GetMarketplaceListingsQuery, PaginatedResult<MarketplaceListingDto>>
{
    public async System.Threading.Tasks.Task<PaginatedResult<MarketplaceListingDto>> Handle(GetMarketplaceListingsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.MarketplaceListings.AsNoTracking().AsQueryable();
        if (request.Status.HasValue)
        {
            query = query.Where(listing => listing.Status == request.Status.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(listing => listing.Task != null
                && (listing.Task.Title.ToLower().Contains(search)
                    || (listing.Task.Description != null && listing.Task.Description.ToLower().Contains(search))));
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var rows = await query.OrderByDescending(listing => listing.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(listing => new ListingRow(
                listing.Id,
                listing.TaskId,
                listing.Status,
                listing.ApprovalMode,
                listing.PublishedAt,
                listing.ExpiresAt,
                listing.ConcurrencyToken,
                listing.Task == null ? null : listing.Task.Title,
                listing.Task == null ? null : listing.Task.Description,
                listing.Task == null ? null : listing.Task.CategoryId,
                listing.Task != null && listing.Task.Category != null ? listing.Task.Category.Name : null,
                listing.Task == null ? null : listing.Task.Priority,
                listing.Task == null ? null : listing.Task.Deadline,
                listing.Task == null ? null : listing.Task.EstimatedDurationMinutes))
            .ToListAsync(cancellationToken);

        var taskIds = rows.Select(row => row.TaskId).Distinct().ToArray();
        var skills = await dbContext.TaskRequiredSkills.AsNoTracking()
            .Where(skill => taskIds.Contains(skill.TaskId))
            .OrderBy(skill => skill.Skill == null ? string.Empty : skill.Skill.Name)
            .Select(skill => new SkillRow(skill.TaskId, skill.SkillId, skill.Skill == null ? string.Empty : skill.Skill.Name, skill.MinimumLevel))
            .ToListAsync(cancellationToken);
        var skillsByTask = skills.GroupBy(skill => skill.TaskId).ToDictionary(group => group.Key, group => (IReadOnlyCollection<MarketplaceRequiredSkillSummaryDto>)group.Select(skill => new MarketplaceRequiredSkillSummaryDto(skill.SkillId, skill.SkillName, skill.MinimumLevel)).ToArray());

        var items = rows.Select(row => new MarketplaceListingDto(
            row.Id,
            row.TaskId,
            row.Status,
            row.ApprovalMode,
            row.PublishedAt,
            row.ExpiresAt,
            row.ConcurrencyToken,
            row.TaskTitle is null || row.CategoryId is null || row.Priority is null || row.Deadline is null || row.EstimatedDurationMinutes is null
                ? null
                : new MarketplaceTaskSummaryDto(row.TaskId, row.TaskTitle, row.TaskDescription, row.CategoryId.Value, row.CategoryName, row.Priority.Value, row.Deadline.Value, row.EstimatedDurationMinutes.Value, skillsByTask.GetValueOrDefault(row.TaskId, Array.Empty<MarketplaceRequiredSkillSummaryDto>()))))
            .ToArray();

        return new PaginatedResult<MarketplaceListingDto>(items, page, pageSize, totalCount, totalPages, page < totalPages, page > 1);
    }

    private sealed record ListingRow(Guid Id, Guid TaskId, MarketplaceListingStatus Status, MarketplaceApprovalMode ApprovalMode, DateTimeOffset? PublishedAt, DateTimeOffset? ExpiresAt, Guid ConcurrencyToken, string? TaskTitle, string? TaskDescription, Guid? CategoryId, string? CategoryName, TaskPriority? Priority, DateTimeOffset? Deadline, int? EstimatedDurationMinutes);
    private sealed record SkillRow(Guid TaskId, Guid SkillId, string SkillName, SkillLevel MinimumLevel);
}
