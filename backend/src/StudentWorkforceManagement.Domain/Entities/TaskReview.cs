using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskReview : AuditableEntity
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid SubmissionId { get; set; }
    public TaskSubmission? Submission { get; set; }
    public Guid ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public string? ReviewerComment { get; set; }
    public bool IsApproved { get; set; }
}
