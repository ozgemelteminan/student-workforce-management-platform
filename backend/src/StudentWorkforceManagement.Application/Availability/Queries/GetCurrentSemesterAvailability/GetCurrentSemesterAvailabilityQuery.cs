using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Availability.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Availability.Queries.GetCurrentSemesterAvailability;

public sealed record GetCurrentSemesterAvailabilityQuery(Guid StudentId) : IRequest<IReadOnlyCollection<AvailabilityDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetCurrentSemesterAvailabilityQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetCurrentSemesterAvailabilityQuery, IReadOnlyCollection<AvailabilityDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<AvailabilityDto>> Handle(GetCurrentSemesterAvailabilityQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new ForbiddenException("Students may view only their own availability.");
        }
        var activeSemesterId = await dbContext.Semesters.AsNoTracking().Where(semester => semester.Status == SemesterStatus.ACTIVE).Select(semester => (Guid?)semester.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("No active semester exists.");
        return await dbContext.Availability.AsNoTracking().Where(item => item.StudentId == request.StudentId && item.SemesterId == activeSemesterId).OrderBy(item => item.DayOfWeek).ThenBy(item => item.StartTime).Select(item => new AvailabilityDto(item.Id, item.StudentId, item.SemesterId, item.DayOfWeek, item.StartTime, item.EndTime, item.Status, item.Reason, item.ConcurrencyToken)).ToListAsync(cancellationToken);
    }
}
