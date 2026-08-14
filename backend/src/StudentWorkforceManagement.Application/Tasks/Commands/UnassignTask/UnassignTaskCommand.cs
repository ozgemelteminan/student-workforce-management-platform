using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Commands.UnassignTask;

public sealed record UnassignTaskCommand(Guid TaskId, string Reason, Guid? StudentId = null) : IRequest<TaskDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class UnassignTaskCommandValidator : AbstractValidator<UnassignTaskCommand>
{
    public UnassignTaskCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class UnassignTaskCommandHandler(IApplicationDbContext dbContext, IAuditService auditService, IUtcClock clock) : IRequestHandler<UnassignTaskCommand, TaskDto>
{
    public async System.Threading.Tasks.Task<TaskDto> Handle(UnassignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(entity => entity.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
        var activeQuery = dbContext.TaskAssignmentHistory.Where(entity => entity.TaskId == request.TaskId && entity.IsActive);
        if (request.StudentId.HasValue)
        {
            activeQuery = activeQuery.Where(entity => entity.StudentId == request.StudentId.Value);
        }
        var current = await activeQuery.FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("Task has no active assignment to unassign.");

        current.IsActive = false;
        current.UnassignedAt = clock.UtcNow;
        current.Status = AssignmentStatus.UNASSIGNED;
        current.Reason = request.Reason;
        if (task.AssignedStudentId == current.StudentId)
        {
            task.AssignedStudentId = await dbContext.TaskAssignmentHistory
                .Where(entity => entity.TaskId == request.TaskId && entity.IsActive && entity.StudentId != current.StudentId)
                .OrderBy(entity => entity.AssignedAt)
                .Select(entity => (Guid?)entity.StudentId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        await auditService.RecordAsync("TaskUnassigned", "Task", task.Id, oldValue: current.StudentId.ToString(), cancellationToken: cancellationToken);
        return TaskMappings.ToDto(task);
    }
}
