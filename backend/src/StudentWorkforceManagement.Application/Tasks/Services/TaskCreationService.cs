using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public sealed class TaskCreationService(IApplicationDbContext dbContext, IAuditService auditService) : ITaskCreationService
{
    public async System.Threading.Tasks.Task<TaskDto> CreateAsync(
        string title,
        string? description,
        Guid categoryId,
        Guid? semesterId,
        TaskPriority priority,
        TaskDifficulty difficulty,
        DateTimeOffset? startDate,
        DateTimeOffset deadline,
        int estimatedDurationMinutes,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description,
            CategoryId = categoryId,
            SemesterId = semesterId,
            Priority = priority,
            Difficulty = difficulty,
            Status = TaskStatus.ASSIGNED,
            CreatedById = createdById,
            StartDate = startDate,
            Deadline = deadline.ToUniversalTime(),
            EstimatedDurationMinutes = estimatedDurationMinutes
        };

        dbContext.Tasks.Add(task);
        await auditService.RecordAsync("TaskCreated", nameof(DomainTask), task.Id, newValue: task.Title, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TaskMappings.ToDto(task);
    }
}
