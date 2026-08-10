using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Task : Entity, ISoftDeletable, IHasConcurrencyToken
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskDifficulty Difficulty { get; set; }
    public StudentWorkforceManagement.Domain.Enums.TaskStatus Status { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset Deadline { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
