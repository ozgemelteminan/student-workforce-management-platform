using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class EmailDelivery : Entity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public EmailDeliveryStatus Status { get; set; }
    public int Attempts { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderMessageId { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? FailureReason { get; set; }
}
