using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Task : AuditableEntity, ISoftDeletable, IHasConcurrencyToken
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? SemesterId { get; set; }
    public Semester? Semester { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskDifficulty Difficulty { get; set; }
    public StudentWorkforceManagement.Domain.Enums.TaskStatus Status { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public Guid? AssignedStudentId { get; set; }
    public Student? AssignedStudent { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset Deadline { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }

    public ICollection<TaskAssignmentHistory> AssignmentHistory { get; set; } = new List<TaskAssignmentHistory>();
    public ICollection<TaskRequiredSkill> RequiredSkills { get; set; } = new List<TaskRequiredSkill>();
    public ICollection<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();
    public ICollection<TaskDependency> DependentTasks { get; set; } = new List<TaskDependency>();
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskChecklistItem> ChecklistItems { get; set; } = new List<TaskChecklistItem>();
    public ICollection<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
    public ICollection<TaskRequest> Requests { get; set; } = new List<TaskRequest>();
    public ICollection<MarketplaceListing> MarketplaceListings { get; set; } = new List<MarketplaceListing>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
}
