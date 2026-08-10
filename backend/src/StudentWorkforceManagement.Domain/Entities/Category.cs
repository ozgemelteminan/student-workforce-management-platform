using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Task> Tasks { get; set; } = new List<Task>();
    public ICollection<TaskTemplate> TaskTemplates { get; set; } = new List<TaskTemplate>();
}
