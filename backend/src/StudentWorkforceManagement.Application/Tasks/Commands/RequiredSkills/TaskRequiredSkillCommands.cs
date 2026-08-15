using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Commands.RequiredSkills;

public sealed record AddTaskRequiredSkillCommand(Guid TaskId, Guid SkillId, SkillLevel MinimumLevel) : IRequest<TaskRequiredSkillDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record UpdateTaskRequiredSkillCommand(Guid TaskId, Guid SkillId, SkillLevel MinimumLevel) : IRequest<TaskRequiredSkillDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record DeleteTaskRequiredSkillCommand(Guid TaskId, Guid SkillId) : IRequest<Unit>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class AddTaskRequiredSkillCommandValidator : AbstractValidator<AddTaskRequiredSkillCommand>
{
    public AddTaskRequiredSkillCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.SkillId).NotEmpty();
        RuleFor(command => command.MinimumLevel).IsInEnum();
    }
}

public sealed class UpdateTaskRequiredSkillCommandValidator : AbstractValidator<UpdateTaskRequiredSkillCommand>
{
    public UpdateTaskRequiredSkillCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.SkillId).NotEmpty();
        RuleFor(command => command.MinimumLevel).IsInEnum();
    }
}

public sealed class DeleteTaskRequiredSkillCommandValidator : AbstractValidator<DeleteTaskRequiredSkillCommand>
{
    public DeleteTaskRequiredSkillCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.SkillId).NotEmpty();
    }
}

public sealed class TaskRequiredSkillCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<AddTaskRequiredSkillCommand, TaskRequiredSkillDto>,
      IRequestHandler<UpdateTaskRequiredSkillCommand, TaskRequiredSkillDto>,
      IRequestHandler<DeleteTaskRequiredSkillCommand, Unit>
{
    public async System.Threading.Tasks.Task<TaskRequiredSkillDto> Handle(AddTaskRequiredSkillCommand request, CancellationToken cancellationToken)
    {
        await EnsureTaskExistsAsync(request.TaskId, cancellationToken);
        var skill = await GetSkillAsync(request.SkillId, cancellationToken);
        var exists = await dbContext.TaskRequiredSkills.AnyAsync(item => item.TaskId == request.TaskId && item.SkillId == request.SkillId, cancellationToken);
        if (exists)
        {
            throw new ConflictException("Task already requires this skill.");
        }

        var requiredSkill = new TaskRequiredSkill
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            SkillId = request.SkillId,
            MinimumLevel = request.MinimumLevel
        };
        dbContext.TaskRequiredSkills.Add(requiredSkill);
        return ToDto(requiredSkill, skill.Name);
    }

    public async System.Threading.Tasks.Task<TaskRequiredSkillDto> Handle(UpdateTaskRequiredSkillCommand request, CancellationToken cancellationToken)
    {
        await EnsureTaskExistsAsync(request.TaskId, cancellationToken);
        var skill = await GetSkillAsync(request.SkillId, cancellationToken);
        var requiredSkill = await dbContext.TaskRequiredSkills.SingleOrDefaultAsync(item => item.TaskId == request.TaskId && item.SkillId == request.SkillId, cancellationToken)
            ?? throw new NotFoundException("TaskRequiredSkill", request.SkillId);

        requiredSkill.MinimumLevel = request.MinimumLevel;
        return ToDto(requiredSkill, skill.Name);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteTaskRequiredSkillCommand request, CancellationToken cancellationToken)
    {
        await EnsureTaskExistsAsync(request.TaskId, cancellationToken);
        var requiredSkill = await dbContext.TaskRequiredSkills.SingleOrDefaultAsync(item => item.TaskId == request.TaskId && item.SkillId == request.SkillId, cancellationToken)
            ?? throw new NotFoundException("TaskRequiredSkill", request.SkillId);

        dbContext.TaskRequiredSkills.Remove(requiredSkill);
        return Unit.Value;
    }

    private async System.Threading.Tasks.Task EnsureTaskExistsAsync(Guid taskId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Tasks.AnyAsync(task => task.Id == taskId, cancellationToken))
        {
            throw new NotFoundException("Task", taskId);
        }
    }

    private async System.Threading.Tasks.Task<Skill> GetSkillAsync(Guid skillId, CancellationToken cancellationToken)
    {
        var skill = await dbContext.Skills.AsNoTracking().SingleOrDefaultAsync(skill => skill.Id == skillId, cancellationToken)
            ?? throw new NotFoundException("Skill", skillId);
        if (!skill.IsActive)
        {
            throw new ConflictException("Inactive skills cannot be selected.");
        }

        return skill;
    }

    private static TaskRequiredSkillDto ToDto(TaskRequiredSkill skill, string? skillName)
    {
        return new TaskRequiredSkillDto(skill.Id, skill.TaskId, skill.SkillId, skillName, skill.MinimumLevel);
    }
}
