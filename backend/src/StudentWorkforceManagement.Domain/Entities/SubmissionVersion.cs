using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class SubmissionVersion : AuditableEntity, ISoftDeletable
{
    public Guid TaskSubmissionId { get; set; }
    public TaskSubmission? TaskSubmission { get; set; }
    public int VersionNumber { get; set; }
    public FileMetadata File { get; set; } = new();
    public FileStatus FileStatus { get; set; }
    public Guid UploadedById { get; set; }
    public Student? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
