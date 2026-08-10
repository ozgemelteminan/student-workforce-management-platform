namespace StudentWorkforceManagement.Domain.Enums;

public enum EmailDeliveryStatus
{
    QUEUED,
    PROCESSING,
    SENT,
    FAILED,
    DELIVERED,
    BOUNCED,
}
