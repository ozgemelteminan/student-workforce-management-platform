using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class DepartmentFile : AuditableEntity, ISoftDeletable
{
    public Guid? FolderId { get; set; }
    public FileFolder? Folder { get; set; }
    public Guid UploadedById { get; set; }
    public User? UploadedBy { get; set; }
    public FileMetadata File { get; set; } = new();
    public FileStatus FileStatus { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
