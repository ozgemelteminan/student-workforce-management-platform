using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class ExportRequest : AuditableEntity, IHasConcurrencyToken
{
    public Guid RequestingUserId { get; set; }
    public User? RequestingUser { get; set; }
    public ExportType ExportType { get; set; }
    public ExportFormat Format { get; set; }
    public ExportStatus Status { get; set; } = ExportStatus.QUEUED;
    public Guid? ScopeId { get; set; }
    public Guid? AuthorizedUserId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? FailureReason { get; set; }
    public string? ArtifactStorageKey { get; set; }
    public string? ArtifactFileName { get; set; }
    public long? ArtifactFileSize { get; set; }
    public string? ArtifactMimeType { get; set; }
    public string? ArtifactContentHash { get; set; }
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    public Guid ConcurrencyToken { get; set; }
}
