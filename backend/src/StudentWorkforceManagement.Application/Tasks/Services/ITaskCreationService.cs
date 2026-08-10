using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public interface ITaskCreationService
{
    System.Threading.Tasks.Task<TaskDto> CreateAsync(
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
        CancellationToken cancellationToken = default);
}
