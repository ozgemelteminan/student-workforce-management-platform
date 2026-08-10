using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Application.RecurringTasks.Services;

public interface IRecurringTaskGenerationService
{
    System.Threading.Tasks.Task<TaskDto> GenerateAsync(RecurringTask recurringTask, DateTimeOffset scheduledRunAt, CancellationToken cancellationToken = default);
}
