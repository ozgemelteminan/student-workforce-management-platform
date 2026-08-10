using Hangfire;
using Microsoft.Extensions.Logging;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.DataExport;

public sealed class DataExportJob(ILogger<DataExportJob> logger)
{
    [AutomaticRetry(Attempts = 1)]
    public Task<int> RunAsync(Guid exportRequestId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DataExportJob received export request {ExportRequestId}, but durable export request persistence is deferred to the API/export workflow phase.", exportRequestId);
        return Task.FromResult(0);
    }
}
