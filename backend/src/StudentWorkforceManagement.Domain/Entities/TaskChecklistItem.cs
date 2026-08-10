using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskChecklistItem : Entity
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public int Order { get; set; }
}
