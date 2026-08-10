using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Persistence;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.SemesterRollover;

public sealed class SemesterRolloverJob(ApplicationDbContext dbContext, IUtcClock clock, IOptions<BackgroundJobOptions> options, ILogger<SemesterRolloverJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var active = await dbContext.Semesters
            .Where(semester => semester.Status == SemesterStatus.ACTIVE && semester.EndDate < today)
            .OrderBy(semester => semester.EndDate)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);
        foreach (var semester in active)
        {
            semester.Status = SemesterStatus.ARCHIVED;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("SemesterRolloverJob archived {Count} semesters", active.Count);
        return active.Count;
    }
}
