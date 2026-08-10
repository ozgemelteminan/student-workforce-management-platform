using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Events;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Tasks.DTOs;
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
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Tasks.Commands.AssignTask;

public sealed record AssignTaskCommand(Guid TaskId, Guid StudentId, string? Reason = null)
    : IRequest<TaskDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class AssignTaskCommandValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.Reason).MaximumLength(1000);
    }
}

public sealed class AssignTaskCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IAuditService auditService, IApplicationEventQueue events, IUtcClock clock)
    : IRequestHandler<AssignTaskCommand, TaskDto>
{
    public async System.Threading.Tasks.Task<TaskDto> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(entity => entity.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
        var student = await dbContext.Students.SingleOrDefaultAsync(entity => entity.Id == request.StudentId, cancellationToken)
            ?? throw new NotFoundException("Student", request.StudentId);

        if (!student.IsActive)
        {
            throw new ConflictException("Inactive students cannot receive tasks.");
        }

        if (await dbContext.TaskAssignmentHistory.AnyAsync(entity => entity.TaskId == request.TaskId && entity.IsActive, cancellationToken))
        {
            throw new ConflictException("Task already has an active assignment.");
        }

        var actorId = currentUser.RequireUserId();
        task.AssignedStudentId = student.Id;
        task.Status = TaskStatus.ASSIGNED;
        dbContext.TaskAssignmentHistory.Add(new TaskAssignmentHistory
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            StudentId = student.Id,
            AssignedByUserId = actorId,
            AssignedAt = clock.UtcNow,
            Status = AssignmentStatus.ACTIVE,
            Mode = AssignmentMode.MANUAL,
            IsActive = true,
            Reason = request.Reason
        });

        await auditService.RecordAsync("TaskAssigned", "Task", task.Id, newValue: student.Id.ToString(), cancellationToken: cancellationToken);
        events.Enqueue(new TaskAssignedEvent(task.Id, student.Id, actorId));
        return TaskMappings.ToDto(task);
    }
}
