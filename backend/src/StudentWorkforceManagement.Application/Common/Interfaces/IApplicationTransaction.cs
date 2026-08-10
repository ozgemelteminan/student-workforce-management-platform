namespace StudentWorkforceManagement.Application.Common.Interfaces;

public interface IApplicationTransaction : IAsyncDisposable
{
    System.Threading.Tasks.Task CommitAsync(CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task RollbackAsync(CancellationToken cancellationToken = default);
}
