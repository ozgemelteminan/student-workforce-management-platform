using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.Persistence;
using DomainTaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.DeadlineReminders;

public sealed class DeadlineReminderJob(
    ApplicationDbContext dbContext,
    INotificationIntentService notifications,
    IUtcClock clock,
    IOptions<BackgroundJobOptions> options,
    ILogger<DeadlineReminderJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var batchSize = options.Value.BatchSize;
        var windowEnd = now.AddHours(24);
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.AssignedStudentId != null && task.Deadline > now && task.Deadline <= windowEnd &&
                task.Status != DomainTaskStatus.COMPLETED && task.Status != DomainTaskStatus.CANCELLED && task.Status != DomainTaskStatus.OVERDUE)
            .OrderBy(task => task.Deadline)
            .Take(batchSize)
            .Select(task => new { task.Id, task.Title, task.Deadline, StudentUserId = task.AssignedStudent!.UserId })
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            var key = task.Deadline <= now.AddHours(3) ? $"TASK_{task.Id:N}_DEADLINE_3H" : $"TASK_{task.Id:N}_DEADLINE_24H";
            await notifications.CreateAsync(task.StudentUserId, NotificationType.DEADLINE_REMINDER, "Task deadline reminder", $"Task '{task.Title}' is due at {task.Deadline:O}.", "Task", task.Id, key, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("DeadlineReminderJob considered {Count} tasks", tasks.Count);
        return tasks.Count;
    }
}
