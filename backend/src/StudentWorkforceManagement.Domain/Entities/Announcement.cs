using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Announcement : Entity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsPinned { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
