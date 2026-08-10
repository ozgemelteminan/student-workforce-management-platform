using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskChecklistItem : AuditableEntity
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedById { get; set; }
    public User? CompletedBy { get; set; }
    public int Order { get; set; }
}
