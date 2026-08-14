using StudentWorkforceManagement.Domain.Common;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskNudge : AuditableEntity
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid SenderStudentId { get; set; }
    public Student? SenderStudent { get; set; }
    public Guid RecipientStudentId { get; set; }
    public Student? RecipientStudent { get; set; }
    public DateTimeOffset SentAt { get; set; }
}
