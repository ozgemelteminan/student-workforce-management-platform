using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Announcements.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Announcements.Queries.GetAnnouncement;

public sealed record GetAnnouncementQuery(Guid Id) : IRequest<AnnouncementDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetAnnouncementQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext) : IRequestHandler<GetAnnouncementQuery, AnnouncementDto>
{
    public async System.Threading.Tasks.Task<AnnouncementDto> Handle(GetAnnouncementQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Announcements.AsNoTracking().Where(announcement => announcement.Id == request.Id).Select(announcement => new AnnouncementDto(announcement.Id, announcement.Title, announcement.Content, announcement.CreatedById, announcement.ExpiresAt, announcement.IsPinned, announcement.IsPublished, announcement.PublishedAt, announcement.CreatedAt, announcement.UpdatedAt)).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Announcement", request.Id);
    }
}
