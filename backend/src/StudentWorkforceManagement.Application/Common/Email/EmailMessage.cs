namespace StudentWorkforceManagement.Application.Common.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string TemplateKey,
    IReadOnlyDictionary<string, string> TemplateData,
    string IdempotencyKey,
    IReadOnlyDictionary<string, string>? SecretTemplateData = null);
