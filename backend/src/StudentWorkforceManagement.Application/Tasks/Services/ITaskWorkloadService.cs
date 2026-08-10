namespace StudentWorkforceManagement.Application.Tasks.Services;

public interface ITaskWorkloadService
{
    System.Threading.Tasks.Task<int> GetActiveWorkloadMinutesAsync(Guid studentId, Guid? semesterId = null, CancellationToken cancellationToken = default);
}
