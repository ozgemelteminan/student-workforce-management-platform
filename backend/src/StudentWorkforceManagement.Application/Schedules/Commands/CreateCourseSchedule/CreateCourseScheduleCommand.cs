using FluentValidation;
using MediatR;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Schedules.DTOs;
using Announcement = StudentWorkforceManagement.Domain.Entities.Announcement;
using AuditLog = StudentWorkforceManagement.Domain.Entities.AuditLog;
using AvailabilityEntity = StudentWorkforceManagement.Domain.Entities.Availability;
using Category = StudentWorkforceManagement.Domain.Entities.Category;
using CourseSchedule = StudentWorkforceManagement.Domain.Entities.CourseSchedule;
using EmailDelivery = StudentWorkforceManagement.Domain.Entities.EmailDelivery;
using MarketplaceClaim = StudentWorkforceManagement.Domain.Entities.MarketplaceClaim;
using MarketplaceListing = StudentWorkforceManagement.Domain.Entities.MarketplaceListing;
using Notification = StudentWorkforceManagement.Domain.Entities.Notification;
using NotificationPreference = StudentWorkforceManagement.Domain.Entities.NotificationPreference;
using Semester = StudentWorkforceManagement.Domain.Entities.Semester;
using Skill = StudentWorkforceManagement.Domain.Entities.Skill;
using StudentSkill = StudentWorkforceManagement.Domain.Entities.StudentSkill;
using SubmissionVersion = StudentWorkforceManagement.Domain.Entities.SubmissionVersion;
using SystemSetting = StudentWorkforceManagement.Domain.Entities.SystemSetting;
using TaskAssignmentHistory = StudentWorkforceManagement.Domain.Entities.TaskAssignmentHistory;
using TaskChecklistItem = StudentWorkforceManagement.Domain.Entities.TaskChecklistItem;
using TaskComment = StudentWorkforceManagement.Domain.Entities.TaskComment;
using TaskDependency = StudentWorkforceManagement.Domain.Entities.TaskDependency;
using TaskRequest = StudentWorkforceManagement.Domain.Entities.TaskRequest;
using TaskReview = StudentWorkforceManagement.Domain.Entities.TaskReview;
using TaskSubmission = StudentWorkforceManagement.Domain.Entities.TaskSubmission;
using DepartmentFile = StudentWorkforceManagement.Domain.Entities.DepartmentFile;
using Feedback = StudentWorkforceManagement.Domain.Entities.Feedback;
using FileFolder = StudentWorkforceManagement.Domain.Entities.FileFolder;
using Invitation = StudentWorkforceManagement.Domain.Entities.Invitation;
using RecurringTask = StudentWorkforceManagement.Domain.Entities.RecurringTask;
using RefreshToken = StudentWorkforceManagement.Domain.Entities.RefreshToken;
using Role = StudentWorkforceManagement.Domain.Entities.Role;
using Session = StudentWorkforceManagement.Domain.Entities.Session;
using Student = StudentWorkforceManagement.Domain.Entities.Student;
using TaskRequiredSkill = StudentWorkforceManagement.Domain.Entities.TaskRequiredSkill;
using TaskTemplate = StudentWorkforceManagement.Domain.Entities.TaskTemplate;
using User = StudentWorkforceManagement.Domain.Entities.User;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Schedules.Commands.CreateCourseSchedule;

public sealed record CreateCourseScheduleCommand(Guid StudentId, Guid SemesterId, string CourseName, string CourseCode, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string? Location)
    : IRequest<CourseScheduleDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class CreateCourseScheduleCommandValidator : AbstractValidator<CreateCourseScheduleCommand>
{
    public CreateCourseScheduleCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.SemesterId).NotEmpty();
        RuleFor(command => command.CourseName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.CourseCode).NotEmpty().MaximumLength(50);
        RuleFor(command => command).Must(command => command.EndTime > command.StartTime).WithMessage("Course end time must be after start time.");
    }
}
public sealed class CreateCourseScheduleCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<CreateCourseScheduleCommand, CourseScheduleDto>
{
    public async System.Threading.Tasks.Task<CourseScheduleDto> Handle(CreateCourseScheduleCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new StudentWorkforceManagement.Application.Common.Exceptions.ForbiddenException("Students may edit only their own schedule.");
        }
        var schedule = new CourseSchedule { Id = Guid.NewGuid(), StudentId = request.StudentId, SemesterId = request.SemesterId, CourseName = request.CourseName.Trim(), CourseCode = request.CourseCode.Trim(), DayOfWeek = request.DayOfWeek, StartTime = request.StartTime, EndTime = request.EndTime, Location = request.Location };
        dbContext.CourseSchedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CourseScheduleDto(schedule.Id, schedule.StudentId, schedule.SemesterId, schedule.CourseName, schedule.CourseCode, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime, schedule.Location);
    }
}
