using FluentValidation;
using MediatR;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Application.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    Guid CategoryId,
    Guid? SemesterId,
    TaskPriority Priority,
    TaskDifficulty Difficulty,
    DateTimeOffset? StartDate,
    DateTimeOffset Deadline,
    int EstimatedDurationMinutes) : IRequest<TaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(8000);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Priority).IsInEnum();
        RuleFor(command => command.Difficulty).IsInEnum();
        RuleFor(command => command.EstimatedDurationMinutes).GreaterThan(0);
        RuleFor(command => command.Deadline).Must(deadline => deadline.Offset == TimeSpan.Zero).WithMessage("Deadline must be provided as a UTC DateTimeOffset.");
        RuleFor(command => command).Must(command => !command.StartDate.HasValue || command.Deadline > command.StartDate.Value).WithMessage("Deadline must be after start date.");
    }
}

public sealed class CreateTaskCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IAuditService auditService)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public async System.Threading.Tasks.Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            CategoryId = request.CategoryId,
            SemesterId = request.SemesterId,
            Priority = request.Priority,
            Difficulty = request.Difficulty,
            Status = TaskStatus.ASSIGNED,
            CreatedById = currentUser.RequireUserId(),
            StartDate = request.StartDate,
            Deadline = request.Deadline.ToUniversalTime(),
            EstimatedDurationMinutes = request.EstimatedDurationMinutes
        };

        dbContext.Tasks.Add(task);
        await auditService.RecordAsync("TaskCreated", nameof(DomainTask), task.Id, newValue: task.Title, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TaskMappings.ToDto(task);
    }
}
