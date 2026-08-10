using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Auth.DTOs;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Auth.Queries;

public sealed record GetInvitationsQuery : PagedQuery, IRequest<PaginatedResult<InvitationDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record GetActiveSessionsQuery(Guid? UserId = null) : IRequest<IReadOnlyCollection<SessionDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class AuthQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetInvitationsQuery, PaginatedResult<InvitationDto>>, IRequestHandler<GetActiveSessionsQuery, IReadOnlyCollection<SessionDto>>
{
    public async System.Threading.Tasks.Task<PaginatedResult<InvitationDto>> Handle(GetInvitationsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Invitations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(invitation => invitation.Email.ToLower().Contains(term));
        }
        var projected = query.OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new InvitationDto(invitation.Id, invitation.Email, invitation.ExpiresAt, invitation.AcceptedAt, invitation.RevokedAt, invitation.CreatedById, invitation.CreatedAt));
        return await projected.ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<SessionDto>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? currentUser.RequireUserId();
        if (userId != currentUser.UserId && !currentUser.IsInRole(UserRole.ADMIN))
        {
            throw new StudentWorkforceManagement.Application.Common.Exceptions.ForbiddenException("Users may view only their own sessions.");
        }
        return await dbContext.Sessions.AsNoTracking()
            .Where(session => session.UserId == userId && session.RevokedAt == null)
            .OrderByDescending(session => session.CreatedAt)
            .Select(session => new SessionDto(session.Id, session.UserId, session.DeviceName, session.IpAddress, session.ExpiresAt, session.RevokedAt, session.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
