using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.Commands.CreateTask;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Application.Templates.DTOs;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Templates.Commands;

public sealed record CreateTaskTemplateCommand(string Title, string? Description, Guid CategoryId, TaskPriority DefaultPriority, TaskDifficulty DefaultDifficulty, int EstimatedDurationMinutes, string? ChecklistTemplateJson, string? RequiredSkillsTemplateJson) : IRequest<TaskTemplateDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record UpdateTaskTemplateCommand(Guid TemplateId, string Title, string? Description, Guid CategoryId, TaskPriority DefaultPriority, TaskDifficulty DefaultDifficulty, int EstimatedDurationMinutes, string? ChecklistTemplateJson, string? RequiredSkillsTemplateJson) : IRequest<TaskTemplateDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record DeleteTaskTemplateCommand(Guid TemplateId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record CreateTaskFromTemplateCommand(Guid TemplateId, DateTimeOffset? StartDate, DateTimeOffset Deadline, Guid? SemesterId = null) : IRequest<TaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class CreateTaskTemplateCommandValidator : AbstractValidator<CreateTaskTemplateCommand>
{
    public CreateTaskTemplateCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(8000);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.DefaultPriority).IsInEnum();
        RuleFor(command => command.DefaultDifficulty).IsInEnum();
        RuleFor(command => command.EstimatedDurationMinutes).GreaterThan(0);
    }
}

public sealed class UpdateTaskTemplateCommandValidator : AbstractValidator<UpdateTaskTemplateCommand>
{
    public UpdateTaskTemplateCommandValidator()
    {
        RuleFor(command => command.TemplateId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(8000);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.EstimatedDurationMinutes).GreaterThan(0);
    }
}

public sealed class CreateTaskFromTemplateCommandValidator : AbstractValidator<CreateTaskFromTemplateCommand>
{
    public CreateTaskFromTemplateCommandValidator()
    {
        RuleFor(command => command.TemplateId).NotEmpty();
        RuleFor(command => command.Deadline).Must(value => value.Offset == TimeSpan.Zero).WithMessage("Deadline must be UTC.");
        RuleFor(command => command).Must(command => !command.StartDate.HasValue || command.Deadline > command.StartDate.Value).WithMessage("Deadline must be after start date.");
    }
}

public sealed class TaskTemplateCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IRequestHandler<CreateTaskCommand, TaskDto> createTaskHandler)
    : IRequestHandler<CreateTaskTemplateCommand, TaskTemplateDto>, IRequestHandler<UpdateTaskTemplateCommand, TaskTemplateDto>, IRequestHandler<DeleteTaskTemplateCommand, Unit>, IRequestHandler<CreateTaskFromTemplateCommand, TaskDto>
{
    public async System.Threading.Tasks.Task<TaskTemplateDto> Handle(CreateTaskTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = new TaskTemplate
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            CategoryId = request.CategoryId,
            DefaultPriority = request.DefaultPriority,
            DefaultDifficulty = request.DefaultDifficulty,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            CreatedById = currentUser.RequireUserId(),
            ChecklistTemplateJson = request.ChecklistTemplateJson,
            RequiredSkillsTemplateJson = request.RequiredSkillsTemplateJson
        };
        dbContext.TaskTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(template);
    }

    public async System.Threading.Tasks.Task<TaskTemplateDto> Handle(UpdateTaskTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await dbContext.TaskTemplates.SingleOrDefaultAsync(entity => entity.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException("TaskTemplate", request.TemplateId);
        template.Title = request.Title.Trim();
        template.Description = request.Description;
        template.CategoryId = request.CategoryId;
        template.DefaultPriority = request.DefaultPriority;
        template.DefaultDifficulty = request.DefaultDifficulty;
        template.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        template.ChecklistTemplateJson = request.ChecklistTemplateJson;
        template.RequiredSkillsTemplateJson = request.RequiredSkillsTemplateJson;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(template);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteTaskTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await dbContext.TaskTemplates.SingleOrDefaultAsync(entity => entity.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException("TaskTemplate", request.TemplateId);
        if (await dbContext.RecurringTasks.AnyAsync(recurring => recurring.TemplateId == request.TemplateId, cancellationToken))
        {
            throw new ConflictException("Templates used by recurring tasks cannot be deleted.");
        }
        dbContext.TaskTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async System.Threading.Tasks.Task<TaskDto> Handle(CreateTaskFromTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await dbContext.TaskTemplates.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException("TaskTemplate", request.TemplateId);
        return await createTaskHandler.Handle(new CreateTaskCommand(template.Title, template.Description, template.CategoryId, request.SemesterId, template.DefaultPriority, template.DefaultDifficulty, request.StartDate, request.Deadline, template.EstimatedDurationMinutes), cancellationToken);
    }

    private static TaskTemplateDto ToDto(TaskTemplate template) => new(template.Id, template.Title, template.Description, template.CategoryId, template.DefaultPriority, template.DefaultDifficulty, template.EstimatedDurationMinutes, template.CreatedById, template.ChecklistTemplateJson, template.RequiredSkillsTemplateJson, template.CreatedAt, template.UpdatedAt);
}
