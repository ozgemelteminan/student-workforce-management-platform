using Hangfire;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs;

public static class JobScheduleRegistrar
{
    public static void RegisterRecurringJobs(IRecurringJobManager recurringJobs)
    {
        recurringJobs.AddOrUpdate<DeadlineReminders.DeadlineReminderJob>("deadline-reminders", job => job.RunAsync(CancellationToken.None), "*/15 * * * *");
        recurringJobs.AddOrUpdate<OverdueTasks.OverdueTaskJob>("overdue-tasks", job => job.RunAsync(CancellationToken.None), "*/15 * * * *");
        recurringJobs.AddOrUpdate<EmailDispatch.EmailDispatchJob>("email-dispatch", job => job.RunAsync(CancellationToken.None), "*/2 * * * *");
        recurringJobs.AddOrUpdate<RecurringTasks.RecurringTaskJob>("recurring-tasks", job => job.RunAsync(CancellationToken.None), "*/15 * * * *");
        recurringJobs.AddOrUpdate<MarketplaceClaimExpiration.MarketplaceClaimExpirationJob>("marketplace-claim-expiration", job => job.RunAsync(CancellationToken.None), "*/10 * * * *");
        recurringJobs.AddOrUpdate<WeeklyTimesheets.WeeklyTimesheetReminderJob>("weekly-timesheet-reminders", job => job.RunAsync(CancellationToken.None), "0 18 * * 0");
        recurringJobs.AddOrUpdate<SemesterRollover.SemesterRolloverJob>("semester-rollover", job => job.RunAsync(CancellationToken.None), "0 1 * * *");
        recurringJobs.AddOrUpdate<OrphanFileCleanup.OrphanFileCleanupJob>("orphan-file-cleanup", job => job.RunAsync(CancellationToken.None), "0 * * * *");
        recurringJobs.AddOrUpdate<RetentionCleanup.RetentionCleanupJob>("retention-cleanup", job => job.RunAsync(CancellationToken.None), "30 2 * * *");
    }
}
