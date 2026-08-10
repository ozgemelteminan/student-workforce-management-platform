using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.Persistence;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.OrphanFileCleanup;

public sealed class OrphanFileCleanupJob(ApplicationDbContext dbContext, IUtcClock clock, IOptions<BackgroundJobOptions> options, ILogger<OrphanFileCleanupJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = clock.UtcNow.AddMinutes(-options.Value.PendingUploadGraceMinutes);
        var departmentFiles = await dbContext.DepartmentFiles
            .Where(file => file.FileStatus == FileStatus.UPLOAD_PENDING && file.CreatedAt < cutoff)
            .OrderBy(file => file.CreatedAt)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);
        var submissionVersions = await dbContext.SubmissionVersions
            .Where(version => version.FileStatus == FileStatus.UPLOAD_PENDING && version.CreatedAt < cutoff)
            .OrderBy(version => version.CreatedAt)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);
        foreach (var file in departmentFiles)
        {
            file.FileStatus = FileStatus.FAILED;
        }
        foreach (var version in submissionVersions)
        {
            version.FileStatus = FileStatus.FAILED;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        var count = departmentFiles.Count + submissionVersions.Count;
        logger.LogInformation("OrphanFileCleanupJob marked {Count} stale pending uploads failed", count);
        return count;
    }
}
