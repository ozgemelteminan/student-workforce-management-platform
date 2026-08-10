using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.RecurringTasks.DTOs;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.RecurringTasks.Commands;

public sealed record CreateRecurringTaskCommand(Guid TemplateId, string Frequency, string TimeZoneId, TimeOnly? LocalRunTime, DateTimeOffset NextRunAt) : IRequest<RecurringTaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record UpdateRecurringTaskCommand(Guid RecurringTaskId, string Frequency, string TimeZoneId, TimeOnly? LocalRunTime, DateTimeOffset NextRunAt) : IRequest<RecurringTaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record ActivateRecurringTaskCommand(Guid RecurringTaskId) : IRequest<RecurringTaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record DeactivateRecurringTaskCommand(Guid RecurringTaskId) : IRequest<RecurringTaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record DeleteRecurringTaskCommand(Guid RecurringTaskId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class CreateRecurringTaskCommandValidator : AbstractValidator<CreateRecurringTaskCommand>
{
    public CreateRecurringTaskCommandValidator()
    {
        RuleFor(command => command.TemplateId).NotEmpty();
        RuleFor(command => command.Frequency).NotEmpty().MaximumLength(120);
        RuleFor(command => command.TimeZoneId).NotEmpty().MaximumLength(120);
        RuleFor(command => command.NextRunAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("NextRunAt must be UTC.");
    }
}

public sealed class UpdateRecurringTaskCommandValidator : AbstractValidator<UpdateRecurringTaskCommand>
{
    public UpdateRecurringTaskCommandValidator()
    {
        RuleFor(command => command.RecurringTaskId).NotEmpty();
        RuleFor(command => command.Frequency).NotEmpty().MaximumLength(120);
        RuleFor(command => command.TimeZoneId).NotEmpty().MaximumLength(120);
        RuleFor(command => command.NextRunAt).Must(value => value.Offset == TimeSpan.Zero).WithMessage("NextRunAt must be UTC.");
    }
}

public sealed class RecurringTaskCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<CreateRecurringTaskCommand, RecurringTaskDto>, IRequestHandler<UpdateRecurringTaskCommand, RecurringTaskDto>, IRequestHandler<ActivateRecurringTaskCommand, RecurringTaskDto>, IRequestHandler<DeactivateRecurringTaskCommand, RecurringTaskDto>, IRequestHandler<DeleteRecurringTaskCommand, Unit>
{
    public async System.Threading.Tasks.Task<RecurringTaskDto> Handle(CreateRecurringTaskCommand request, CancellationToken cancellationToken)
    {
        if (!await dbContext.TaskTemplates.AnyAsync(template => template.Id == request.TemplateId, cancellationToken))
        {
            throw new NotFoundException("TaskTemplate", request.TemplateId);
        }
        var recurring = new RecurringTask { Id = Guid.NewGuid(), TemplateId = request.TemplateId, Frequency = request.Frequency.Trim(), TimeZoneId = request.TimeZoneId.Trim(), LocalRunTime = request.LocalRunTime, NextRunAt = request.NextRunAt.ToUniversalTime(), IsActive = true, CreatedById = currentUser.RequireUserId() };
        dbContext.RecurringTasks.Add(recurring);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(recurring);
    }

    public async System.Threading.Tasks.Task<RecurringTaskDto> Handle(UpdateRecurringTaskCommand request, CancellationToken cancellationToken)
    {
        var recurring = await dbContext.RecurringTasks.SingleOrDefaultAsync(entity => entity.Id == request.RecurringTaskId, cancellationToken)
            ?? throw new NotFoundException("RecurringTask", request.RecurringTaskId);
        recurring.Frequency = request.Frequency.Trim();
        recurring.TimeZoneId = request.TimeZoneId.Trim();
        recurring.LocalRunTime = request.LocalRunTime;
        recurring.NextRunAt = request.NextRunAt.ToUniversalTime();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(recurring);
    }

    public System.Threading.Tasks.Task<RecurringTaskDto> Handle(ActivateRecurringTaskCommand request, CancellationToken cancellationToken) => SetActiveAsync(request.RecurringTaskId, true, cancellationToken);
    public System.Threading.Tasks.Task<RecurringTaskDto> Handle(DeactivateRecurringTaskCommand request, CancellationToken cancellationToken) => SetActiveAsync(request.RecurringTaskId, false, cancellationToken);

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteRecurringTaskCommand request, CancellationToken cancellationToken)
    {
        var recurring = await dbContext.RecurringTasks.SingleOrDefaultAsync(entity => entity.Id == request.RecurringTaskId, cancellationToken)
            ?? throw new NotFoundException("RecurringTask", request.RecurringTaskId);
        dbContext.RecurringTasks.Remove(recurring);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private async System.Threading.Tasks.Task<RecurringTaskDto> SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken)
    {
        var recurring = await dbContext.RecurringTasks.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException("RecurringTask", id);
        recurring.IsActive = active;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(recurring);
    }

    private static RecurringTaskDto ToDto(RecurringTask recurring) => new(recurring.Id, recurring.TemplateId, recurring.Frequency, recurring.TimeZoneId, recurring.LocalRunTime, recurring.NextRunAt, recurring.IsActive, recurring.CreatedById, recurring.ConcurrencyToken, recurring.CreatedAt, recurring.UpdatedAt);
}
