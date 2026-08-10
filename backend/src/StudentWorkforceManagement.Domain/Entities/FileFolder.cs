using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class FileFolder : AuditableEntity, ISoftDeletable
{
    public Guid? ParentFolderId { get; set; }
    public FileFolder? ParentFolder { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<FileFolder> ChildFolders { get; set; } = new List<FileFolder>();
    public ICollection<DepartmentFile> Files { get; set; } = new List<DepartmentFile>();
}
