using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Semesters.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Semesters.Queries.GetSemesters;

public sealed record GetSemestersQuery : IRequest<IReadOnlyCollection<SemesterDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetActiveSemesterQuery : IRequest<SemesterDto?>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class GetSemestersQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext)
    : IRequestHandler<GetSemestersQuery, IReadOnlyCollection<SemesterDto>>, IRequestHandler<GetActiveSemesterQuery, SemesterDto?>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<SemesterDto>> Handle(GetSemestersQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Semesters.AsNoTracking().OrderByDescending(semester => semester.StartDate).Select(semester => new SemesterDto(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.Status, semester.ConcurrencyToken)).ToListAsync(cancellationToken);
    }

    public System.Threading.Tasks.Task<SemesterDto?> Handle(GetActiveSemesterQuery request, CancellationToken cancellationToken)
    {
        return dbContext.Semesters.AsNoTracking().Where(semester => semester.Status == SemesterStatus.ACTIVE).Select(semester => new SemesterDto(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.Status, semester.ConcurrencyToken)).SingleOrDefaultAsync(cancellationToken);
    }
}
