namespace StudentWorkforceManagement.Application.Tasks.DTOs;

public sealed record AssignmentRecommendationDto(
    Guid StudentId,
    string StudentName,
    int Score,
    int SkillScore,
    int AvailabilityScore,
    int WorkloadScore,
    int PreviousExperienceScore,
    int ActiveWorkloadMinutes,
    IReadOnlyCollection<string> Reasons);
