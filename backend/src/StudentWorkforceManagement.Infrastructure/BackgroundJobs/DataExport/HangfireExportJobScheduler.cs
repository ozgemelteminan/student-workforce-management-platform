using Hangfire;
using StudentWorkforceManagement.Application.Common.Interfaces;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.DataExport;

public sealed class HangfireExportJobScheduler(IBackgroundJobClient jobs) : IExportJobScheduler
{
    public Task EnqueueAsync(Guid exportRequestId, CancellationToken cancellationToken = default)
    {
        jobs.Enqueue<DataExportJob>(job => job.RunAsync(exportRequestId, CancellationToken.None));
        return Task.CompletedTask;
    }
}
