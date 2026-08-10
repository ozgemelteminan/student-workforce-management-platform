using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Application.Tasks.Services;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Tasks.Commands.TransitionTask;

public abstract record TaskTransitionCommand(Guid TaskId, TaskStatus TargetStatus) : IRequest<TaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record AcceptTaskCommand(Guid TaskId) : TaskTransitionCommand(TaskId, TaskStatus.ACCEPTED);
public sealed record StartTaskCommand(Guid TaskId) : TaskTransitionCommand(TaskId, TaskStatus.IN_PROGRESS);
public sealed record SubmitTaskCommand(Guid TaskId) : TaskTransitionCommand(TaskId, TaskStatus.SUBMITTED_FOR_REVIEW);
public sealed record CancelTaskCommand(Guid TaskId, string Reason) : TaskTransitionCommand(TaskId, TaskStatus.CANCELLED)
{
    public new IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class TaskTransitionCommandValidator : AbstractValidator<TaskTransitionCommand>
{
    public TaskTransitionCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.TargetStatus).IsInEnum();
        RuleFor(command => command).Must(command => command is not CancelTaskCommand cancel || !string.IsNullOrWhiteSpace(cancel.Reason)).WithMessage("Cancellation reason is mandatory.");
    }
}

public sealed class TaskTransitionCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, ITaskStateMachine stateMachine, IAuditService auditService)
    : IRequestHandler<AcceptTaskCommand, TaskDto>, IRequestHandler<StartTaskCommand, TaskDto>, IRequestHandler<SubmitTaskCommand, TaskDto>, IRequestHandler<CancelTaskCommand, TaskDto>
{
    public System.Threading.Tasks.Task<TaskDto> Handle(AcceptTaskCommand request, CancellationToken cancellationToken) => HandleTransition(request, cancellationToken);
    public System.Threading.Tasks.Task<TaskDto> Handle(StartTaskCommand request, CancellationToken cancellationToken) => HandleTransition(request, cancellationToken);
    public System.Threading.Tasks.Task<TaskDto> Handle(SubmitTaskCommand request, CancellationToken cancellationToken) => HandleTransition(request, cancellationToken);
    public System.Threading.Tasks.Task<TaskDto> Handle(CancelTaskCommand request, CancellationToken cancellationToken) => HandleTransition(request, cancellationToken);

    private async System.Threading.Tasks.Task<TaskDto> HandleTransition(TaskTransitionCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(entity => entity.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        var isAssignedStudent = task.AssignedStudentId.HasValue && currentUser.StudentId == task.AssignedStudentId.Value;
        if (request.TargetStatus == TaskStatus.IN_PROGRESS && task.Status == TaskStatus.ACCEPTED)
        {
            var incompleteDependencyIds = await dbContext.TaskDependencies.AsNoTracking()
                .Where(dependency => dependency.TaskId == task.Id)
                .Join(dbContext.Tasks.AsNoTracking(), dependency => dependency.DependsOnTaskId, dependencyTask => dependencyTask.Id, (dependency, dependencyTask) => new { dependency.DependsOnTaskId, dependencyTask.Status })
                .Where(dependency => dependency.Status != TaskStatus.COMPLETED)
                .Select(dependency => dependency.DependsOnTaskId)
                .ToListAsync(cancellationToken);

            if (incompleteDependencyIds.Count > 0)
            {
                throw new ConflictException("Task cannot start while dependencies are incomplete.");
            }
        }

        stateMachine.ValidateTransition(task.Status, request.TargetStatus, currentUser.Roles, isAssignedStudent);
        task.Status = request.TargetStatus;
        if (request is CancelTaskCommand cancel)
        {
            task.CancellationReason = cancel.Reason;
        }

        await auditService.RecordAsync("TaskStatusChanged", "Task", task.Id, newValue: request.TargetStatus.ToString(), cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TaskMappings.ToDto(task);
    }
}
