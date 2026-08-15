using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Semesters.DTOs;

public sealed record SemesterDto(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, SemesterStatus Status, Guid ConcurrencyToken, bool IsActive = true);
