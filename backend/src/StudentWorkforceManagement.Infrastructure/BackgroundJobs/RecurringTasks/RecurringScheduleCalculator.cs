using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.RecurringTasks;

public sealed class RecurringScheduleCalculator : IRecurringScheduleCalculator
{
    private static readonly IReadOnlyDictionary<string, DayOfWeek> Weekdays = new Dictionary<string, DayOfWeek>(StringComparer.OrdinalIgnoreCase)
    {
        ["SUNDAY"] = DayOfWeek.Sunday,
        ["MONDAY"] = DayOfWeek.Monday,
        ["TUESDAY"] = DayOfWeek.Tuesday,
        ["WEDNESDAY"] = DayOfWeek.Wednesday,
        ["THURSDAY"] = DayOfWeek.Thursday,
        ["FRIDAY"] = DayOfWeek.Friday,
        ["SATURDAY"] = DayOfWeek.Saturday,
    };

    public DateTimeOffset CalculateNextRun(RecurringTask recurringTask, DateTimeOffset scheduledRunAtUtc)
    {
        var timeZone = FindTimeZone(recurringTask.TimeZoneId);
        var local = TimeZoneInfo.ConvertTime(scheduledRunAtUtc, timeZone);
        var localRunTime = recurringTask.LocalRunTime ?? TimeOnly.FromDateTime(local.DateTime);
        var frequency = recurringTask.Frequency.Trim();
        var nextLocalDate = ResolveNextDate(frequency, DateOnly.FromDateTime(local.DateTime));
        var nextLocal = nextLocalDate.ToDateTime(localRunTime, DateTimeKind.Unspecified);
        return ConvertLocalToUtc(nextLocal, timeZone);
    }

    private static DateOnly ResolveNextDate(string frequency, DateOnly currentLocalDate)
    {
        var normalized = frequency.Trim().ToUpperInvariant();
        if (normalized is "DAILY" or "EVERY DAY")
        {
            return currentLocalDate.AddDays(1);
        }

        if (normalized is "WEEKLY" or "EVERY WEEK")
        {
            return currentLocalDate.AddDays(7);
        }

        if (normalized is "MONTHLY" or "EVERY MONTH")
        {
            return AddOneMonthClamped(currentLocalDate);
        }

        foreach (var weekday in Weekdays)
        {
            if (normalized == weekday.Key || normalized == $"EVERY {weekday.Key}")
            {
                var daysUntil = ((int)weekday.Value - (int)currentLocalDate.DayOfWeek + 7) % 7;
                return currentLocalDate.AddDays(daysUntil == 0 ? 7 : daysUntil);
            }
        }

        throw new InvalidOperationException($"Unsupported recurring frequency '{frequency}'.");
    }

    private static DateOnly AddOneMonthClamped(DateOnly date)
    {
        var targetMonth = date.Month == 12 ? 1 : date.Month + 1;
        var targetYear = date.Month == 12 ? date.Year + 1 : date.Year;
        var targetDay = Math.Min(date.Day, DateTime.DaysInMonth(targetYear, targetMonth));
        return new DateOnly(targetYear, targetMonth, targetDay);
    }

    private static DateTimeOffset ConvertLocalToUtc(DateTime local, TimeZoneInfo timeZone)
    {
        var normalizedLocal = local;
        while (timeZone.IsInvalidTime(normalizedLocal))
        {
            normalizedLocal = normalizedLocal.AddMinutes(1);
        }

        var offset = timeZone.GetUtcOffset(normalizedLocal);
        return new DateTimeOffset(normalizedLocal, offset).ToUniversalTime();
    }

    private static TimeZoneInfo FindTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new InvalidOperationException($"Unknown recurring task time zone '{timeZoneId}'.", ex);
        }
        catch (InvalidTimeZoneException ex)
        {
            throw new InvalidOperationException($"Invalid recurring task time zone '{timeZoneId}'.", ex);
        }
    }
}
