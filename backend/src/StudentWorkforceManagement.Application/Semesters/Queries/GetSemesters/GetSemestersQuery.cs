using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Semesters.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Semesters.Queries.GetSemesters;

public sealed record GetSemestersQuery(bool IncludeInactive = false) : IRequest<IReadOnlyCollection<SemesterDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetActiveSemesterQuery : IRequest<SemesterDto?>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class GetSemestersQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetSemestersQuery, IReadOnlyCollection<SemesterDto>>, IRequestHandler<GetActiveSemesterQuery, SemesterDto?>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<SemesterDto>> Handle(GetSemestersQuery request, CancellationToken cancellationToken)
    {
        var includeInactive = request.IncludeInactive && currentUser.IsInRole(UserRole.ADMIN);
        var query = dbContext.Semesters.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(semester => semester.IsActive);
        }
        return await query.OrderByDescending(semester => semester.StartDate).Select(semester => new SemesterDto(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.Status, semester.ConcurrencyToken, semester.IsActive)).ToListAsync(cancellationToken);
    }

    public System.Threading.Tasks.Task<SemesterDto?> Handle(GetActiveSemesterQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Semesters.AsNoTracking().Where(semester => semester.Status == SemesterStatus.ACTIVE && semester.IsActive).Select(semester => new SemesterDto(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.Status, semester.ConcurrencyToken, semester.IsActive)).SingleOrDefaultAsync(cancellationToken);
    }
}
