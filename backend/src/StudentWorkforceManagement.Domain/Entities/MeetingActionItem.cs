using StudentWorkforceManagement.Domain.Common;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class MeetingActionItem : AuditableEntity, IHasConcurrencyToken
{
    public Guid MeetingId { get; set; }
    public Meeting? Meeting { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? AssignedStudentId { get; set; }
    public Student? AssignedStudent { get; set; }
    public Guid? TaskId { get; set; }
    public Task? Task { get; set; }
    public bool IsCompleted { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
