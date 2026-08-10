using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskRequiredSkill : Entity
{
    public Guid TaskId { get; set; }
    public Guid SkillId { get; set; }
    public SkillLevel MinimumLevel { get; set; }
}
