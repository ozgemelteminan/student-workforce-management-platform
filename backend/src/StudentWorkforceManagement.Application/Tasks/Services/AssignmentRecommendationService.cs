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
            .Select(entity => new { entity.Id, entity.EstimatedDurationMinutes, entity.SemesterId })
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
                Skills = student.Skills.Select(skill => new { skill.SkillId, skill.Level }).ToList(),
                AvailabilityMinutes = student.Availability
                    .Where(availability => !task.SemesterId.HasValue || availability.SemesterId == task.SemesterId.Value)
                    .Where(availability => availability.Status == AvailabilityStatus.AVAILABLE)
                    .Select(availability => availability.EndTime.ToTimeSpan().TotalMinutes - availability.StartTime.ToTimeSpan().TotalMinutes)
                    .Sum(),
                PreviousAssignments = student.AssignmentHistory.Count(history => history.TaskId == taskId)
            })
            .ToListAsync(cancellationToken);

        var results = new List<AssignmentRecommendationDto>();
        foreach (var student in students)
        {
            var workloadMinutes = await workloadService.GetActiveWorkloadMinutesAsync(student.Id, task.SemesterId, cancellationToken);
            var requiredMatches = requiredSkills.Count == 0
                ? 1
                : requiredSkills.Count(required => student.Skills.Any(skill => skill.SkillId == required.SkillId && skillMatchingService.MeetsMinimumLevel(skill.Level, required.MinimumLevel)));

            var skillScore = requiredSkills.Count == 0 ? 100 : requiredMatches * 100 / requiredSkills.Count;
            var availabilityScore = student.AvailabilityMinutes >= task.EstimatedDurationMinutes ? 100 : student.AvailabilityMinutes <= 0 ? 0 : (int)(student.AvailabilityMinutes * 100 / Math.Max(1, task.EstimatedDurationMinutes));
            var workloadScore = workloadMinutes <= 0 ? 100 : Math.Max(0, 100 - workloadMinutes / 10);
            var previousExperienceScore = student.PreviousAssignments > 0 ? 100 : 50;
            var totalScore = (skillScore * 40 + availabilityScore * 30 + workloadScore * 20 + previousExperienceScore * 10) / 100;
            var reasons = new[]
            {
                $"Skill Match: {skillScore}%",
                $"Availability: {availabilityScore}%",
                $"Workload: {workloadScore}%",
                $"Previous Experience: {previousExperienceScore}%"
            };

            results.Add(new AssignmentRecommendationDto(student.Id, $"{student.FirstName} {student.LastName}".Trim(), totalScore, skillScore, availabilityScore, workloadScore, previousExperienceScore, workloadMinutes, reasons));
        }

        return results.OrderByDescending(result => result.Score).ThenBy(result => result.StudentName).ToArray();
    }
}
