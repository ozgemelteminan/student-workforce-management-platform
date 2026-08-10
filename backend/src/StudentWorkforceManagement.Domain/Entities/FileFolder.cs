using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class FileFolder : Entity, ISoftDeletable
{
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }
}
