using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.RecurringTasks;

public interface IRecurringScheduleCalculator
{
    DateTimeOffset CalculateNextRun(RecurringTask recurringTask, DateTimeOffset scheduledRunAtUtc);
}
