using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class RecurringTaskOccurrence : AuditableEntity
{
    public Guid RecurringTaskId { get; set; }
    public RecurringTask? RecurringTask { get; set; }
    public DateTimeOffset ScheduledRunAt { get; set; }
    public Guid? GeneratedTaskId { get; set; }
    public Task? GeneratedTask { get; set; }
    public RecurringTaskOccurrenceStatus Status { get; set; } = RecurringTaskOccurrenceStatus.PROCESSING;
    public int Attempts { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? FailureReason { get; set; }
}
