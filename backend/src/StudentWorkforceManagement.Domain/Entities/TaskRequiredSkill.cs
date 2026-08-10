using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskRequiredSkill : AuditableEntity
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid SkillId { get; set; }
    public Skill? Skill { get; set; }
    public SkillLevel MinimumLevel { get; set; }
}
