using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskComment : Entity, ISoftDeletable
{
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Visibility { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
