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
using StudentWorkforceManagement.Application.Submissions.DTOs;
using StudentWorkforceManagement.Application.Tasks.Services;
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

namespace StudentWorkforceManagement.Application.Submissions.Commands.ReviewSubmission;

public sealed record ApproveSubmissionCommand(Guid SubmissionId, string? ReviewerComment = null) : IRequest<TaskReviewDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Reviewers;
}
public sealed record RequestSubmissionRevisionCommand(Guid SubmissionId, string ReviewerComment) : IRequest<TaskReviewDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Reviewers;
}

public sealed class RequestSubmissionRevisionCommandValidator : AbstractValidator<RequestSubmissionRevisionCommand>
{
    public RequestSubmissionRevisionCommandValidator()
    {
        RuleFor(command => command.SubmissionId).NotEmpty();
        RuleFor(command => command.ReviewerComment).NotEmpty().MaximumLength(2000);
    }
}

public sealed class ReviewSubmissionCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, ITaskStateMachine stateMachine, IAuditService auditService, IApplicationEventQueue events, IUtcClock clock)
    : IRequestHandler<ApproveSubmissionCommand, TaskReviewDto>, IRequestHandler<RequestSubmissionRevisionCommand, TaskReviewDto>
{
    public System.Threading.Tasks.Task<TaskReviewDto> Handle(ApproveSubmissionCommand request, CancellationToken cancellationToken) => ReviewAsync(request.SubmissionId, true, request.ReviewerComment, cancellationToken);
    public System.Threading.Tasks.Task<TaskReviewDto> Handle(RequestSubmissionRevisionCommand request, CancellationToken cancellationToken) => ReviewAsync(request.SubmissionId, false, request.ReviewerComment, cancellationToken);

    private async System.Threading.Tasks.Task<TaskReviewDto> ReviewAsync(Guid submissionId, bool approved, string? reviewerComment, CancellationToken cancellationToken)
    {
        var submission = await dbContext.TaskSubmissions.SingleOrDefaultAsync(entity => entity.Id == submissionId, cancellationToken)
            ?? throw new NotFoundException("TaskSubmission", submissionId);
        var task = await dbContext.Tasks.SingleOrDefaultAsync(entity => entity.Id == submission.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", submission.TaskId);

        if (currentUser.StudentId == submission.SubmittedById)
        {
            throw new ForbiddenException("A user cannot review their own submitted work.");
        }
        if (task.Status != TaskStatus.SUBMITTED_FOR_REVIEW)
        {
            throw new ConflictException("Only submitted tasks can be reviewed.");
        }

        var targetStatus = approved ? TaskStatus.COMPLETED : TaskStatus.IN_PROGRESS;
        stateMachine.ValidateTransition(task.Status, targetStatus, currentUser.Roles, isAssignedStudent: false, isSelfReview: false);
        task.Status = targetStatus;
        task.CompletedAt = approved ? clock.UtcNow : null;
        submission.Status = approved ? SubmissionStatus.APPROVED : SubmissionStatus.REVISION_REQUESTED;

        var review = new TaskReview
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            SubmissionId = submission.Id,
            ReviewedById = currentUser.RequireUserId(),
            ReviewerComment = reviewerComment,
            IsApproved = approved
        };
        dbContext.TaskReviews.Add(review);
        await auditService.RecordAsync(approved ? "SubmissionApproved" : "SubmissionRevisionRequested", "TaskSubmission", submission.Id, cancellationToken: cancellationToken);
        events.Enqueue(new SubmissionReviewedEvent(task.Id, submission.Id, approved, review.ReviewedById));
        return new TaskReviewDto(review.Id, review.TaskId, review.SubmissionId, review.ReviewedById, review.IsApproved, review.ReviewerComment, review.CreatedAt);
    }
}
