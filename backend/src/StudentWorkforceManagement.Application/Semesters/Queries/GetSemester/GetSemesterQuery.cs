using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Semesters.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Semesters.Queries.GetSemester;

public sealed record GetSemesterQuery(Guid SemesterId) : IRequest<SemesterDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetSemesterQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext) : IRequestHandler<GetSemesterQuery, SemesterDto>
{
    public async System.Threading.Tasks.Task<SemesterDto> Handle(GetSemesterQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Semesters.AsNoTracking().Where(semester => semester.Id == request.SemesterId).Select(semester => new SemesterDto(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.Status, semester.ConcurrencyToken, semester.IsActive)).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Semester", request.SemesterId);
    }
}
