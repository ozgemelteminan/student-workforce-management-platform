using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Announcements.DTOs;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Announcements.Queries.GetAnnouncements;

public sealed record GetAnnouncementsQuery : PagedQuery, IRequest<PaginatedResult<AnnouncementDto>>, IAuthorizableRequest
{
    public bool PublishedOnly { get; init; } = true;
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class GetAnnouncementsQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, IUtcClock clock) : IRequestHandler<GetAnnouncementsQuery, PaginatedResult<AnnouncementDto>>
{
    public async System.Threading.Tasks.Task<PaginatedResult<AnnouncementDto>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var query = dbContext.Announcements.AsNoTracking().AsQueryable();
        if (request.PublishedOnly)
        {
            query = query.Where(announcement => announcement.IsPublished && (!announcement.ExpiresAt.HasValue || announcement.ExpiresAt > now));
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(announcement => announcement.Title.ToLower().Contains(search) || announcement.Content.ToLower().Contains(search));
        }
        return await query.OrderByDescending(announcement => announcement.IsPinned).ThenByDescending(announcement => announcement.PublishedAt ?? announcement.CreatedAt)
            .Select(announcement => new AnnouncementDto(announcement.Id, announcement.Title, announcement.Content, announcement.CreatedById, announcement.ExpiresAt, announcement.IsPinned, announcement.IsPublished, announcement.PublishedAt, announcement.CreatedAt, announcement.UpdatedAt))
            .ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
