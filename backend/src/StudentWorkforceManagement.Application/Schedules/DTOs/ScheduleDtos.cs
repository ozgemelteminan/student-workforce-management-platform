namespace StudentWorkforceManagement.Application.Schedules.DTOs;

public sealed record CourseScheduleDto(Guid Id, Guid StudentId, Guid SemesterId, string CourseName, string CourseCode, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string? Location);
