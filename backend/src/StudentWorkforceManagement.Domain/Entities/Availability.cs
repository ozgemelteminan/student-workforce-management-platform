using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Availability : AuditableEntity, IHasConcurrencyToken
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid SemesterId { get; set; }
    public Semester? Semester { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public AvailabilityStatus Status { get; set; }
    public string? Reason { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
