using Hangfire;
using Microsoft.Extensions.Logging;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.RetentionCleanup;

public sealed class RetentionCleanupJob(ILogger<RetentionCleanupJob> logger)
{
    [AutomaticRetry(Attempts = 1)]
    public Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("RetentionCleanupJob executed no-op because repository retention documents define review/soft-delete policy but no eligible hard-delete automation yet.");
        return Task.FromResult(0);
    }
}
