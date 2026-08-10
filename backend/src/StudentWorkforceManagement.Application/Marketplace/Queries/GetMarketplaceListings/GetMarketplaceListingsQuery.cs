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

        return await query.OrderByDescending(listing => listing.PublishedAt)
            .Select(listing => new MarketplaceListingDto(listing.Id, listing.TaskId, listing.Status, listing.ApprovalMode, listing.PublishedAt, listing.ExpiresAt, listing.ConcurrencyToken))
            .ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
