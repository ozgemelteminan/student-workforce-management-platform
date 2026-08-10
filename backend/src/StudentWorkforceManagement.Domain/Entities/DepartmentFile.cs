using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class DepartmentFile : Entity, ISoftDeletable
{
    public Guid? FolderId { get; set; }
    public Guid UploadedById { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string? ContentHash { get; set; }
    public FileStatus FileStatus { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
