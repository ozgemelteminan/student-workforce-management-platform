using FluentValidation;
using MediatR;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Application.Tasks.Services;
using StudentWorkforceManagement.Domain.Enums;

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

public sealed class CreateTaskCommandHandler(ICurrentUserService currentUser, ITaskCreationService taskCreation)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public System.Threading.Tasks.Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        return taskCreation.CreateAsync(
            request.Title,
            request.Description,
            request.CategoryId,
            request.SemesterId,
            request.Priority,
            request.Difficulty,
            request.StartDate,
            request.Deadline,
            request.EstimatedDurationMinutes,
            currentUser.RequireUserId(),
            cancellationToken);
    }
}
