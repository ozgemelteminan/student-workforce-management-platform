using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class StudentSkill : Entity
{
    public Guid StudentId { get; set; }
    public Guid SkillId { get; set; }
    public SkillLevel Level { get; set; }
}
