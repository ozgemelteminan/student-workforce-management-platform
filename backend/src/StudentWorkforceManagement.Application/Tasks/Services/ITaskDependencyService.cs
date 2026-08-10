namespace StudentWorkforceManagement.Application.Tasks.Services;

public interface ITaskDependencyService
{
    System.Threading.Tasks.Task<bool> WouldCreateCycleAsync(Guid taskId, Guid dependsOnTaskId, CancellationToken cancellationToken = default);
}
