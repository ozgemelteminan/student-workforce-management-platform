using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskSubmission : AuditableEntity, ISoftDeletable, IHasConcurrencyToken
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid SubmittedById { get; set; }
    public Student? SubmittedBy { get; set; }
    public SubmissionStatus Status { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }

    public ICollection<SubmissionVersion> Versions { get; set; } = new List<SubmissionVersion>();
    public ICollection<TaskReview> Reviews { get; set; } = new List<TaskReview>();
}
