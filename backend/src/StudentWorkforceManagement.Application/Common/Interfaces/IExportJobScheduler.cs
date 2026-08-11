namespace StudentWorkforceManagement.Application.Common.Interfaces;

public interface IExportJobScheduler
{
    System.Threading.Tasks.Task EnqueueAsync(Guid exportRequestId, CancellationToken cancellationToken = default);
}
