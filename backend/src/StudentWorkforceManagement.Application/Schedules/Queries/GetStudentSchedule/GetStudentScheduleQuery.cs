using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Schedules.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Schedules.Queries.GetStudentSchedule;

public sealed record GetStudentScheduleQuery(Guid StudentId, Guid? SemesterId = null) : IRequest<IReadOnlyCollection<CourseScheduleDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class GetStudentScheduleQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetStudentScheduleQuery, IReadOnlyCollection<CourseScheduleDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<CourseScheduleDto>> Handle(GetStudentScheduleQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new StudentWorkforceManagement.Application.Common.Exceptions.ForbiddenException("Students may view only their own schedule.");
        }
        var query = dbContext.CourseSchedules.AsNoTracking().Where(schedule => schedule.StudentId == request.StudentId);
        if (request.SemesterId.HasValue) query = query.Where(schedule => schedule.SemesterId == request.SemesterId.Value);
        return await query.OrderBy(schedule => schedule.DayOfWeek).ThenBy(schedule => schedule.StartTime).Select(schedule => new CourseScheduleDto(schedule.Id, schedule.StudentId, schedule.SemesterId, schedule.CourseName, schedule.CourseCode, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime, schedule.Location)).ToListAsync(cancellationToken);
    }
}
