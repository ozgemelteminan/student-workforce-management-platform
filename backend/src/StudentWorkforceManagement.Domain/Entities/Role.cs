using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Role : AuditableEntity
{
    public UserRole Name { get; set; }
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
