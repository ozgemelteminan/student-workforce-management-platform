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

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.OverdueTasks;

public sealed class OverdueTaskJob(
    ApplicationDbContext dbContext,
    INotificationIntentService notifications,
    IUtcClock clock,
    IOptions<BackgroundJobOptions> options,
    ILogger<OverdueTaskJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var tasks = await dbContext.Tasks
            .Include(task => task.AssignedStudent)
            .Where(task => task.AssignedStudentId != null && task.Deadline < now &&
                task.Status != DomainTaskStatus.COMPLETED && task.Status != DomainTaskStatus.CANCELLED && task.Status != DomainTaskStatus.OVERDUE)
            .OrderBy(task => task.Deadline)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            task.Status = DomainTaskStatus.OVERDUE;
            if (task.AssignedStudent is not null)
            {
                await notifications.CreateAsync(task.AssignedStudent.UserId, NotificationType.OVERDUE, "Task overdue", $"Task '{task.Title}' is overdue.", "Task", task.Id, $"TASK_{task.Id:N}_OVERDUE", cancellationToken);
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("OverdueTaskJob marked {Count} tasks overdue", tasks.Count);
        return tasks.Count;
    }
}
