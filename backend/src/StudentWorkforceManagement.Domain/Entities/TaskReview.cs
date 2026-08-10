using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskReview : Entity
{
    public Guid TaskId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ReviewedById { get; set; }
    public string? ReviewerComment { get; set; }
}
