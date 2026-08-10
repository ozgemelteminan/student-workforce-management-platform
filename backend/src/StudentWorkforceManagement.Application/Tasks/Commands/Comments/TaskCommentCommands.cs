using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

namespace StudentWorkforceManagement.Application.Tasks.Commands.Comments;

public sealed record AddTaskCommentCommand(Guid TaskId, string Content, TaskCommentVisibility Visibility) : IRequest<TaskCommentDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record UpdateTaskCommentCommand(Guid TaskId, Guid CommentId, string Content) : IRequest<TaskCommentDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record DeleteTaskCommentCommand(Guid TaskId, Guid CommentId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class AddTaskCommentCommandValidator : AbstractValidator<AddTaskCommentCommand>
{
    public AddTaskCommentCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty().MaximumLength(8000);
        RuleFor(command => command.Visibility).IsInEnum();
    }
}

public sealed class UpdateTaskCommentCommandValidator : AbstractValidator<UpdateTaskCommentCommand>
{
    public UpdateTaskCommentCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.CommentId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty().MaximumLength(8000);
    }
}

public sealed class TaskCommentCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IAuditService auditService, IUtcClock clock)
    : IRequestHandler<AddTaskCommentCommand, TaskCommentDto>,
      IRequestHandler<UpdateTaskCommentCommand, TaskCommentDto>,
      IRequestHandler<DeleteTaskCommentCommand, Unit>
{
    public async System.Threading.Tasks.Task<TaskCommentDto> Handle(AddTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            if (task.AssignedStudentId != currentUser.StudentId)
            {
                throw new ForbiddenException("Students may only comment on their assigned tasks.");
            }

            if (request.Visibility != TaskCommentVisibility.STUDENT_VISIBLE)
            {
                throw new ForbiddenException("Students cannot create internal comments.");
            }
        }

        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            AuthorId = currentUser.RequireUserId(),
            Content = request.Content.Trim(),
            Visibility = request.Visibility
        };

        dbContext.TaskComments.Add(comment);
        await auditService.RecordAsync("TaskCommentAdded", "Task", task.Id, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(comment);
    }

    public async System.Threading.Tasks.Task<TaskCommentDto> Handle(UpdateTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await dbContext.TaskComments.SingleOrDefaultAsync(entity => entity.Id == request.CommentId && entity.TaskId == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("TaskComment", request.CommentId);

        if (!CanModifyComment(comment.AuthorId))
        {
            throw new ForbiddenException("Only the author or staff may update a comment.");
        }

        comment.Content = request.Content.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(comment);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await dbContext.TaskComments.SingleOrDefaultAsync(entity => entity.Id == request.CommentId && entity.TaskId == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("TaskComment", request.CommentId);

        if (!CanModifyComment(comment.AuthorId))
        {
            throw new ForbiddenException("Only the author or staff may delete a comment.");
        }

        comment.DeletedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private bool CanModifyComment(Guid authorId)
    {
        return currentUser.UserId == authorId || currentUser.IsInRole(UserRole.ADMIN) || currentUser.IsInRole(UserRole.TASK_MANAGER);
    }

    private static TaskCommentDto ToDto(TaskComment comment)
    {
        return new TaskCommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.Content, comment.Visibility, comment.CreatedAt, comment.UpdatedAt);
    }
}
