namespace StudentWorkforceManagement.Application.Students.DTOs;

public sealed record StudentDto(Guid Id, Guid UserId, string FirstName, string LastName, string Email, string Department, int? WeeklyTargetMinutes, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, Guid ConcurrencyToken);

public sealed record StudentProfileDto(StudentDto Student, int ActiveTaskCount, int CompletedTaskCount, int CurrentWorkloadMinutes, int SkillCount, int ScheduleEntryCount, int AvailabilityEntryCount);
