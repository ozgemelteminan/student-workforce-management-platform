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
using StudentWorkforceManagement.Application.Requests.DTOs;
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

namespace StudentWorkforceManagement.Application.Requests.Commands.ReviewTaskRequest;

public sealed record ApproveTaskRequestCommand(Guid RequestId, string? ReviewerComment = null, Guid? NewAssigneeId = null)
    : IRequest<TaskRequestDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record RejectTaskRequestCommand(Guid RequestId, string ReviewerComment) : IRequest<TaskRequestDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record CancelTaskRequestCommand(Guid RequestId) : IRequest<TaskRequestDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed class ApproveTaskRequestCommandValidator : AbstractValidator<ApproveTaskRequestCommand>
{
    public ApproveTaskRequestCommandValidator()
    {
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.ReviewerComment).MaximumLength(2000);
    }
}
public sealed class RejectTaskRequestCommandValidator : AbstractValidator<RejectTaskRequestCommand>
{
    public RejectTaskRequestCommandValidator()
    {
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.ReviewerComment).NotEmpty().MaximumLength(2000);
    }
}

public sealed class ReviewTaskRequestCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IAuditService auditService, IApplicationEventQueue events, IUtcClock clock)
    : IRequestHandler<ApproveTaskRequestCommand, TaskRequestDto>,
      IRequestHandler<RejectTaskRequestCommand, TaskRequestDto>,
      IRequestHandler<CancelTaskRequestCommand, TaskRequestDto>
{
    public async System.Threading.Tasks.Task<TaskRequestDto> Handle(ApproveTaskRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaskRequests.SingleOrDefaultAsync(item => item.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException("TaskRequest", request.RequestId);
        if (entity.Status != RequestStatus.PENDING)
        {
            throw new ConflictException("Only pending task requests can be approved.");
        }

        var task = await dbContext.Tasks.SingleOrDefaultAsync(item => item.Id == entity.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", entity.TaskId);
        var reviewerId = currentUser.RequireUserId();
        entity.Status = RequestStatus.APPROVED;
        entity.ReviewedAt = clock.UtcNow;
        entity.ReviewedById = reviewerId;
        entity.ReviewerComment = request.ReviewerComment;

        if (entity.Type == RequestType.EXTENSION)
        {
            if (!entity.RequestedDeadline.HasValue)
            {
                throw new ConflictException("Extension request is missing a requested deadline.");
            }

            task.Deadline = entity.RequestedDeadline.Value.ToUniversalTime();
        }
        else
        {
            var newAssigneeId = request.NewAssigneeId ?? entity.SuggestedStudentId ?? throw new ConflictException("Approved reassignment requires a new assignee.");
            var currentAssignment = await dbContext.TaskAssignmentHistory.SingleOrDefaultAsync(history => history.TaskId == task.Id && history.IsActive, cancellationToken)
                ?? throw new ConflictException("Task has no active assignment to reassign.");
            currentAssignment.IsActive = false;
            currentAssignment.UnassignedAt = clock.UtcNow;
            currentAssignment.Status = AssignmentStatus.REASSIGNED;
            task.AssignedStudentId = newAssigneeId;
            dbContext.TaskAssignmentHistory.Add(new TaskAssignmentHistory
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                StudentId = newAssigneeId,
                AssignedByUserId = reviewerId,
                AssignedAt = clock.UtcNow,
                Status = AssignmentStatus.ACTIVE,
                Mode = AssignmentMode.REASSIGNMENT,
                IsActive = true,
                Reason = entity.Reason
            });
        }

        await auditService.RecordAsync("TaskRequestApproved", "TaskRequest", entity.Id, cancellationToken: cancellationToken);
        events.Enqueue(new TaskRequestReviewedEvent(entity.Id, entity.TaskId, true, reviewerId));
        return RequestMappings.ToDto(entity);
    }

    public async System.Threading.Tasks.Task<TaskRequestDto> Handle(RejectTaskRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaskRequests.SingleOrDefaultAsync(item => item.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException("TaskRequest", request.RequestId);
        if (entity.Status != RequestStatus.PENDING)
        {
            throw new ConflictException("Only pending task requests can be rejected.");
        }

        entity.Status = RequestStatus.REJECTED;
        entity.ReviewedAt = clock.UtcNow;
        entity.ReviewedById = currentUser.RequireUserId();
        entity.ReviewerComment = request.ReviewerComment.Trim();
        await auditService.RecordAsync("TaskRequestRejected", "TaskRequest", entity.Id, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RequestMappings.ToDto(entity);
    }

    public async System.Threading.Tasks.Task<TaskRequestDto> Handle(CancelTaskRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TaskRequests.SingleOrDefaultAsync(item => item.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException("TaskRequest", request.RequestId);
        if (entity.RequestedById != currentUser.StudentId)
        {
            throw new ForbiddenException("Students may cancel only their own task requests.");
        }
        if (entity.Status != RequestStatus.PENDING)
        {
            throw new ConflictException("Only pending task requests can be cancelled.");
        }

        entity.Status = RequestStatus.CANCELLED;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RequestMappings.ToDto(entity);
    }
}
