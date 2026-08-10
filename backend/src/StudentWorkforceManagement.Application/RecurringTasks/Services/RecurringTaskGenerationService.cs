using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Application.Tasks.Services;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Application.RecurringTasks.Services;

public sealed class RecurringTaskGenerationService(IApplicationDbContext dbContext, ITaskCreationService taskCreation) : IRecurringTaskGenerationService
{
    public async System.Threading.Tasks.Task<TaskDto> GenerateAsync(RecurringTask recurringTask, DateTimeOffset scheduledRunAt, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.TaskTemplates.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == recurringTask.TemplateId, cancellationToken)
            ?? throw new NotFoundException("TaskTemplate", recurringTask.TemplateId);

        var startDate = scheduledRunAt.ToUniversalTime();
        var deadline = startDate.AddDays(7);
        return await taskCreation.CreateAsync(
            template.Title,
            template.Description,
            template.CategoryId,
            null,
            template.DefaultPriority,
            template.DefaultDifficulty,
            startDate,
            deadline,
            template.EstimatedDurationMinutes,
            recurringTask.CreatedById,
            cancellationToken);
    }
}
