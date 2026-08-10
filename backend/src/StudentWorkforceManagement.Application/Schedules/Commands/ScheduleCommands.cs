using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Schedules.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Schedules.Commands;

public sealed record UpdateCourseScheduleCommand(Guid ScheduleId, string CourseName, string CourseCode, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string? Location) : IRequest<CourseScheduleDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record DeleteCourseScheduleCommand(Guid ScheduleId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class UpdateCourseScheduleCommandValidator : AbstractValidator<UpdateCourseScheduleCommand>
{
    public UpdateCourseScheduleCommandValidator()
    {
        RuleFor(command => command.ScheduleId).NotEmpty();
        RuleFor(command => command.CourseName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.CourseCode).NotEmpty().MaximumLength(50);
        RuleFor(command => command).Must(command => command.EndTime > command.StartTime).WithMessage("Course end time must be after start time.");
    }
}

public sealed class ScheduleCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<UpdateCourseScheduleCommand, CourseScheduleDto>, IRequestHandler<DeleteCourseScheduleCommand, Unit>
{
    public async System.Threading.Tasks.Task<CourseScheduleDto> Handle(UpdateCourseScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await dbContext.CourseSchedules.SingleOrDefaultAsync(entity => entity.Id == request.ScheduleId, cancellationToken)
            ?? throw new NotFoundException("CourseSchedule", request.ScheduleId);
        EnsureCanModify(schedule.StudentId);
        schedule.CourseName = request.CourseName.Trim();
        schedule.CourseCode = request.CourseCode.Trim();
        schedule.DayOfWeek = request.DayOfWeek;
        schedule.StartTime = request.StartTime;
        schedule.EndTime = request.EndTime;
        schedule.Location = request.Location;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(schedule);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteCourseScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await dbContext.CourseSchedules.SingleOrDefaultAsync(entity => entity.Id == request.ScheduleId, cancellationToken)
            ?? throw new NotFoundException("CourseSchedule", request.ScheduleId);
        EnsureCanModify(schedule.StudentId);
        dbContext.CourseSchedules.Remove(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private void EnsureCanModify(Guid studentId)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != studentId)
        {
            throw new ForbiddenException("Students may edit only their own schedule.");
        }
        if (!currentUser.IsInRole(UserRole.STUDENT) && !currentUser.IsInRole(UserRole.ADMIN) && !currentUser.IsInRole(UserRole.TASK_MANAGER))
        {
            throw new ForbiddenException("Only students and authorized staff may modify schedules.");
        }
    }

    private static CourseScheduleDto ToDto(StudentWorkforceManagement.Domain.Entities.CourseSchedule schedule) => new(schedule.Id, schedule.StudentId, schedule.SemesterId, schedule.CourseName, schedule.CourseCode, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime, schedule.Location);
}
