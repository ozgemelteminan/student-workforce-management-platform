namespace StudentWorkforceManagement.Application.Common.Email;

public sealed record EmailProviderResult(bool Accepted, string? ProviderMessageId, string? FailureReason);
