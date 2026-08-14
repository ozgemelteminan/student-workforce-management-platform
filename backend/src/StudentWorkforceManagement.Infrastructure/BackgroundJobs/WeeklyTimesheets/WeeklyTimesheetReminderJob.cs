using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Persistence;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.WeeklyTimesheets;

public sealed class WeeklyTimesheetReminderJob(
    ApplicationDbContext dbContext,
    INotificationIntentService notifications,
    IUtcClock clock,
    IOptions<BackgroundJobOptions> options,
    ILogger<WeeklyTimesheetReminderJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var istanbulNow = TimeZoneInfo.ConvertTime(clock.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
        if (istanbulNow.DayOfWeek != DayOfWeek.Sunday)
        {
            return 0;
        }

        var weekStart = WeekStart(DateOnly.FromDateTime(istanbulNow.DateTime));
        var weekEnd = weekStart.AddDays(6);
        var students = await dbContext.Students.AsNoTracking()
            .Where(student => student.IsActive)
            .Where(student => !dbContext.TimesheetWeeks.Any(week => week.StudentId == student.Id && week.WeekStartDate == weekStart && (week.Status == TimesheetStatus.SUBMITTED || week.Status == TimesheetStatus.APPROVED)))
            .OrderBy(student => student.Id)
            .Take(options.Value.BatchSize)
            .Select(student => new { student.Id, student.UserId })
            .ToListAsync(cancellationToken);

        foreach (var student in students)
        {
            await notifications.CreateAsync(
                student.UserId,
                NotificationType.TIMESHEET_REMINDER,
                "Submit weekly hours",
                "Please review and submit your weekly hours.",
                "TimesheetWeek",
                null,
                $"TIMESHEET_REMINDER_{student.Id:N}_{weekStart:yyyyMMdd}_{weekEnd:yyyyMMdd}",
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("WeeklyTimesheetReminderJob notified {Count} students for week {WeekStart}", students.Count, weekStart);
        return students.Count;
    }

    private static DateOnly WeekStart(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
