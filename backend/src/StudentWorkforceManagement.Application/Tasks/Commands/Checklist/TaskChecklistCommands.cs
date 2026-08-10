using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
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

namespace StudentWorkforceManagement.Application.Tasks.Commands.Checklist;

public sealed record AddChecklistItemCommand(Guid TaskId, string Title, int Order) : IRequest<TaskChecklistItemDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record CompleteChecklistItemCommand(Guid TaskId, Guid ItemId) : IRequest<TaskChecklistItemDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record UncompleteChecklistItemCommand(Guid TaskId, Guid ItemId) : IRequest<TaskChecklistItemDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class AddChecklistItemCommandValidator : AbstractValidator<AddChecklistItemCommand>
{
    public AddChecklistItemCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(300);
        RuleFor(command => command.Order).GreaterThanOrEqualTo(0);
    }
}

public sealed class TaskChecklistCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IUtcClock clock)
    : IRequestHandler<AddChecklistItemCommand, TaskChecklistItemDto>,
      IRequestHandler<CompleteChecklistItemCommand, TaskChecklistItemDto>,
      IRequestHandler<UncompleteChecklistItemCommand, TaskChecklistItemDto>
{
    public async System.Threading.Tasks.Task<TaskChecklistItemDto> Handle(AddChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var item = new TaskChecklistItem { Id = Guid.NewGuid(), TaskId = request.TaskId, Title = request.Title.Trim(), Order = request.Order };
        dbContext.TaskChecklistItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public System.Threading.Tasks.Task<TaskChecklistItemDto> Handle(CompleteChecklistItemCommand request, CancellationToken cancellationToken) => SetCompletionAsync(request.TaskId, request.ItemId, true, cancellationToken);
    public System.Threading.Tasks.Task<TaskChecklistItemDto> Handle(UncompleteChecklistItemCommand request, CancellationToken cancellationToken) => SetCompletionAsync(request.TaskId, request.ItemId, false, cancellationToken);

    private async System.Threading.Tasks.Task<TaskChecklistItemDto> SetCompletionAsync(Guid taskId, Guid itemId, bool isCompleted, CancellationToken cancellationToken)
    {
        var item = await dbContext.TaskChecklistItems.SingleOrDefaultAsync(entity => entity.Id == itemId && entity.TaskId == taskId, cancellationToken)
            ?? throw new NotFoundException("TaskChecklistItem", itemId);
        var task = await dbContext.Tasks.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == taskId, cancellationToken)
            ?? throw new NotFoundException("Task", taskId);

        if (currentUser.IsInRole(UserRole.STUDENT) && task.AssignedStudentId != currentUser.StudentId)
        {
            throw new ForbiddenException("Students may only update checklist items for their assigned tasks.");
        }

        item.IsCompleted = isCompleted;
        item.CompletedAt = isCompleted ? clock.UtcNow : null;
        item.CompletedById = isCompleted ? currentUser.UserId : null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    private static TaskChecklistItemDto ToDto(TaskChecklistItem item)
    {
        return new TaskChecklistItemDto(item.Id, item.TaskId, item.Title, item.IsCompleted, item.CompletedAt, item.CompletedById, item.Order);
    }
}
