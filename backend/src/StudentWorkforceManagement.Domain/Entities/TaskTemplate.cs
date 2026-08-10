using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskTemplate : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public TaskPriority DefaultPriority { get; set; }
    public TaskDifficulty DefaultDifficulty { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public string? ChecklistTemplateJson { get; set; }
    public string? RequiredSkillsTemplateJson { get; set; }

    public ICollection<RecurringTask> RecurringTasks { get; set; } = new List<RecurringTask>();
}
