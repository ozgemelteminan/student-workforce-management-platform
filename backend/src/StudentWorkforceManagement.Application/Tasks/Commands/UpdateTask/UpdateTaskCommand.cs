using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(
    Guid Id,
    string Title,
    string? Description,
    Guid CategoryId,
    Guid? SemesterId,
    TaskPriority Priority,
    TaskDifficulty Difficulty,
    DateTimeOffset? StartDate,
    DateTimeOffset Deadline,
    int EstimatedDurationMinutes,
    Guid ConcurrencyToken) : IRequest<TaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(8000);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Priority).IsInEnum();
        RuleFor(command => command.Difficulty).IsInEnum();
        RuleFor(command => command.EstimatedDurationMinutes).GreaterThan(0);
        RuleFor(command => command.ConcurrencyToken).NotEmpty();
        RuleFor(command => command).Must(command => !command.StartDate.HasValue || command.Deadline > command.StartDate.Value).WithMessage("Deadline must be after start date.");
    }
}

public sealed class UpdateTaskCommandHandler(IApplicationDbContext dbContext, IAuditService auditService)
    : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    public async System.Threading.Tasks.Task<TaskDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.SingleOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Task", request.Id);

        if (task.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new ConcurrencyConflictException();
        }
        if (!await dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId && category.IsActive, cancellationToken))
        {
            throw new ConflictException("Inactive or missing categories cannot be selected.");
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.CategoryId = request.CategoryId;
        task.SemesterId = request.SemesterId;
        task.Priority = request.Priority;
        task.Difficulty = request.Difficulty;
        task.StartDate = request.StartDate;
        task.Deadline = request.Deadline.ToUniversalTime();
        task.EstimatedDurationMinutes = request.EstimatedDurationMinutes;

        await auditService.RecordAsync("TaskUpdated", "Task", task.Id, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TaskMappings.ToDto(task);
    }
}
