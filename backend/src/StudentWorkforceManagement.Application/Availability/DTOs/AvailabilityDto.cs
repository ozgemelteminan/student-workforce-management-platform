using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Availability.DTOs;

public sealed record AvailabilityDto(Guid Id, Guid StudentId, Guid SemesterId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, AvailabilityStatus Status, string? Reason, Guid ConcurrencyToken);
