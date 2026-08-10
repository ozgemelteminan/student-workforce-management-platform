using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class StudentSkill : AuditableEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid SkillId { get; set; }
    public Skill? Skill { get; set; }
    public SkillLevel Level { get; set; }
}
