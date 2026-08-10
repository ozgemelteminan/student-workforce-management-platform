using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskSubmission : Entity, ISoftDeletable, IHasConcurrencyToken
{
    public Guid TaskId { get; set; }
    public Guid SubmittedById { get; set; }
    public SubmissionStatus Status { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
