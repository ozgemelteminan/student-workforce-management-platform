using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Announcement : AuditableEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsPinned { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
