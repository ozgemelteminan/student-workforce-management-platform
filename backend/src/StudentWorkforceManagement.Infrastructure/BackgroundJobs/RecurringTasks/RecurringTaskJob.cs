using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.RecurringTasks.Services;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.Persistence;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.RecurringTasks;

public sealed class RecurringTaskJob(
    ApplicationDbContext dbContext,
    IRecurringTaskGenerationService generator,
    IRecurringScheduleCalculator scheduleCalculator,
    IUtcClock clock,
    IOptions<BackgroundJobOptions> options,
    ILogger<RecurringTaskJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var recurringTasks = await dbContext.RecurringTasks
            .Where(item => item.IsActive && item.NextRunAt <= now)
            .OrderBy(item => item.NextRunAt)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var recurring in recurringTasks)
        {
            created += await TryGenerateOccurrenceAsync(recurring, now, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("RecurringTaskJob generated {Count} tasks", created);
        return created;
    }

    private async Task<int> TryGenerateOccurrenceAsync(RecurringTask recurring, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var scheduledRunAt = recurring.NextRunAt.ToUniversalTime();
        var occurrence = await dbContext.RecurringTaskOccurrences
            .SingleOrDefaultAsync(item => item.RecurringTaskId == recurring.Id && item.ScheduledRunAt == scheduledRunAt, cancellationToken);

        if (occurrence?.Status == RecurringTaskOccurrenceStatus.COMPLETED)
        {
            recurring.NextRunAt = scheduleCalculator.CalculateNextRun(recurring, scheduledRunAt);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return 0;
        }

        if (occurrence?.Status == RecurringTaskOccurrenceStatus.PROCESSING)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return 0;
        }

        if (occurrence is not null && occurrence.Attempts >= options.Value.JobRetryAttempts)
        {
            logger.LogWarning("Recurring task occurrence {OccurrenceId} for recurring task {RecurringTaskId} has reached retry limit", occurrence.Id, recurring.Id);
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return 0;
        }

        occurrence ??= new RecurringTaskOccurrence
        {
            Id = Guid.NewGuid(),
            RecurringTaskId = recurring.Id,
            ScheduledRunAt = scheduledRunAt
        };
        if (occurrence.Id == default)
        {
            occurrence.Id = Guid.NewGuid();
        }
        if (dbContext.Entry(occurrence).State == EntityState.Detached)
        {
            dbContext.RecurringTaskOccurrences.Add(occurrence);
        }

        occurrence.Status = RecurringTaskOccurrenceStatus.PROCESSING;
        occurrence.StartedAt = now;
        occurrence.Attempts += 1;
        occurrence.FailedAt = null;
        occurrence.FailureReason = null;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            DetachAddedOccurrences();
            logger.LogInformation(ex, "Recurring task occurrence already claimed for recurring task {RecurringTaskId} at {ScheduledRunAt}", recurring.Id, scheduledRunAt);
            return 0;
        }

        var taskGenerated = false;
        try
        {
            var nextRunAt = scheduleCalculator.CalculateNextRun(recurring, scheduledRunAt);
            var task = await generator.GenerateAsync(recurring, scheduledRunAt, cancellationToken);
            taskGenerated = true;
            occurrence.GeneratedTaskId = task.Id;
            occurrence.Status = RecurringTaskOccurrenceStatus.COMPLETED;
            occurrence.CompletedAt = clock.UtcNow;
            recurring.NextRunAt = nextRunAt;
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (taskGenerated)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }

            occurrence.Status = RecurringTaskOccurrenceStatus.FAILED;
            occurrence.FailedAt = clock.UtcNow;
            occurrence.FailureReason = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            logger.LogWarning(ex, "Recurring task occurrence failed for recurring task {RecurringTaskId} at {ScheduledRunAt}", recurring.Id, scheduledRunAt);
            return 0;
        }
    }

    private void DetachAddedOccurrences()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<RecurringTaskOccurrence>().Where(entry => entry.State == EntityState.Added))
        {
            entry.State = EntityState.Detached;
        }
    }
}
