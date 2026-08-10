using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskTemplate : Entity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public TaskPriority DefaultPriority { get; set; }
    public TaskDifficulty DefaultDifficulty { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public Guid CreatedById { get; set; }
}
