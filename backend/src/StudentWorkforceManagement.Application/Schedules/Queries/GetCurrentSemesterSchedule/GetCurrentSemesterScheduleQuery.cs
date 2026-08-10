using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Schedules.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Schedules.Queries.GetCurrentSemesterSchedule;

public sealed record GetCurrentSemesterScheduleQuery(Guid StudentId) : IRequest<IReadOnlyCollection<CourseScheduleDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetCurrentSemesterScheduleQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetCurrentSemesterScheduleQuery, IReadOnlyCollection<CourseScheduleDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<CourseScheduleDto>> Handle(GetCurrentSemesterScheduleQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new ForbiddenException("Students may view only their own schedule.");
        }
        var activeSemesterId = await dbContext.Semesters.AsNoTracking().Where(semester => semester.Status == SemesterStatus.ACTIVE).Select(semester => (Guid?)semester.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("No active semester exists.");
        return await dbContext.CourseSchedules.AsNoTracking().Where(schedule => schedule.StudentId == request.StudentId && schedule.SemesterId == activeSemesterId).OrderBy(schedule => schedule.DayOfWeek).ThenBy(schedule => schedule.StartTime).Select(schedule => new CourseScheduleDto(schedule.Id, schedule.StudentId, schedule.SemesterId, schedule.CourseName, schedule.CourseCode, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime, schedule.Location)).ToListAsync(cancellationToken);
    }
}
