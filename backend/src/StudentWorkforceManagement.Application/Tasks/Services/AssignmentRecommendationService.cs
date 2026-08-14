using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public sealed class AssignmentRecommendationService(
    IApplicationDbContext dbContext,
    ITaskWorkloadService workloadService,
    ISkillMatchingService skillMatchingService) : IAssignmentRecommendationService
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<AssignmentRecommendationDto>> GetRecommendationsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.Tasks.AsNoTracking()
            .Where(entity => entity.Id == taskId)
            .Select(entity => new { entity.Id, entity.EstimatedDurationMinutes, entity.SemesterId, entity.StartDate, entity.Deadline })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Task", taskId);

        var requiredSkills = await dbContext.TaskRequiredSkills.AsNoTracking()
            .Where(skill => skill.TaskId == taskId)
            .Select(skill => new { skill.SkillId, skill.MinimumLevel })
            .ToListAsync(cancellationToken);

        var students = await dbContext.Students.AsNoTracking()
            .Where(student => student.IsActive)
            .Select(student => new
            {
                student.Id,
                student.FirstName,
                student.LastName,
                student.WeeklyTargetMinutes,
                Skills = student.Skills.Select(skill => new { skill.SkillId, skill.Level }).ToList(),
                AvailabilityMinutes = student.Availability
                    .Where(availability => !task.SemesterId.HasValue || availability.SemesterId == task.SemesterId.Value)
                    .Where(availability => availability.Status == AvailabilityStatus.AVAILABLE)
                    .Select(availability => availability.EndTime.ToTimeSpan().TotalMinutes - availability.StartTime.ToTimeSpan().TotalMinutes)
                    .Sum(),
                PreviousAssignments = student.AssignmentHistory.Count(history => history.TaskId == taskId)
            })
            .ToListAsync(cancellationToken);

        var weekStart = WeekStart(DateOnly.FromDateTime(task.StartDate?.UtcDateTime ?? DateTime.UtcNow));
        var weekEnd = weekStart.AddDays(6);
        var results = new List<AssignmentRecommendationDto>();
        foreach (var student in students)
        {
            var workloadMinutes = await workloadService.GetActiveWorkloadMinutesAsync(student.Id, task.SemesterId, cancellationToken);
            var timesheet = await dbContext.TimesheetWeeks.AsNoTracking()
                .Where(week => week.StudentId == student.Id && week.WeekStartDate == weekStart)
                .Select(week => new { week.TargetMinutes, WorkedMinutes = week.Entries.Sum(entry => entry.Minutes) })
                .SingleOrDefaultAsync(cancellationToken);
            var weeklyTarget = timesheet?.TargetMinutes ?? student.WeeklyTargetMinutes;
            var workedMinutes = timesheet?.WorkedMinutes ?? 0;
            var plannedMinutes = await dbContext.TaskAssignmentHistory.AsNoTracking()
                .Where(history => history.StudentId == student.Id && history.IsActive)
                .SumAsync(history => history.PlannedEffortMinutes ?? 0, cancellationToken);
            var projectedMinutes = workedMinutes + plannedMinutes + task.EstimatedDurationMinutes;
            var unavailable = await dbContext.TemporaryUnavailability.AsNoTracking()
                .AnyAsync(item => item.StudentId == student.Id && item.StartAt < task.Deadline && (!task.StartDate.HasValue || item.EndAt > task.StartDate.Value), cancellationToken);
            var requiredMatches = requiredSkills.Count == 0
                ? 1
                : requiredSkills.Count(required => student.Skills.Any(skill => skill.SkillId == required.SkillId && skillMatchingService.MeetsMinimumLevel(skill.Level, required.MinimumLevel)));

            var skillScore = requiredSkills.Count == 0 ? 100 : requiredMatches * 100 / requiredSkills.Count;
            var availabilityScore = unavailable ? 0 : student.AvailabilityMinutes >= task.EstimatedDurationMinutes ? 100 : student.AvailabilityMinutes <= 0 ? 0 : (int)(student.AvailabilityMinutes * 100 / Math.Max(1, task.EstimatedDurationMinutes));
            var hasWeeklyTarget = weeklyTarget.HasValue && weeklyTarget.Value > 0;
            var capacityPercent = hasWeeklyTarget ? projectedMinutes * 100 / weeklyTarget!.Value : 0;
            var workloadScore = hasWeeklyTarget ? Math.Max(0, 100 - capacityPercent) : 0;
            var previousExperienceScore = student.PreviousAssignments > 0 ? 100 : 50;
            var totalScore = (skillScore * 40 + availabilityScore * 30 + workloadScore * 20 + previousExperienceScore * 10) / 100;
            var reasons = new List<string>
            {
                $"Skill Match: {skillScore}%",
                unavailable ? "Unavailable during this task window" : $"Availability: {availabilityScore}%",
                hasWeeklyTarget ? $"Worked: {workedMinutes} / {weeklyTarget} min" : "target-not-configured",
                hasWeeklyTarget ? $"Projected: {projectedMinutes} / {weeklyTarget} min" : $"Projected: {projectedMinutes} min without configured target",
                $"Active workload: {workloadMinutes} min",
                $"Previous Experience: {previousExperienceScore}%"
            };
            if (hasWeeklyTarget && projectedMinutes > weeklyTarget!.Value)
            {
                reasons.Add("Warning: weekly target would be exceeded.");
            }
            if (skillScore < 100)
            {
                reasons.Add("Warning: required skills are not fully matched.");
            }

            results.Add(new AssignmentRecommendationDto(student.Id, $"{student.FirstName} {student.LastName}".Trim(), totalScore, skillScore, availabilityScore, workloadScore, previousExperienceScore, workloadMinutes, weeklyTarget, workedMinutes, projectedMinutes, reasons));
        }

        return results.OrderByDescending(result => result.Score).ThenBy(result => result.StudentName).ToArray();
    }

    private static DateOnly WeekStart(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
