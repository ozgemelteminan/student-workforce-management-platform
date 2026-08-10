using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class EmailDelivery : AuditableEntity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public EmailDeliveryStatus Status { get; set; }
    public int Attempts { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderMessageId { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? FailureReason { get; set; }
}
