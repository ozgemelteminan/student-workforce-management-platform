using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Skill : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<StudentSkill> StudentSkills { get; set; } = new List<StudentSkill>();
    public ICollection<TaskRequiredSkill> TaskRequiredSkills { get; set; } = new List<TaskRequiredSkill>();
}
