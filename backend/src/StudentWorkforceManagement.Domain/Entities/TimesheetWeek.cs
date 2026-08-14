using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TimesheetWeek : AuditableEntity, IHasConcurrencyToken
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public int TargetMinutes { get; set; }
    public TimesheetStatus Status { get; set; } = TimesheetStatus.DRAFT;
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public string? ReviewerComment { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
}
