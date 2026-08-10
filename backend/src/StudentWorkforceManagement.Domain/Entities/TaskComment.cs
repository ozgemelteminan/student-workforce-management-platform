using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskComment : AuditableEntity, ISoftDeletable
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }
    public string Content { get; set; } = string.Empty;
    public TaskCommentVisibility Visibility { get; set; } = TaskCommentVisibility.STUDENT_VISIBLE;
    public DateTimeOffset? DeletedAt { get; set; }
}
