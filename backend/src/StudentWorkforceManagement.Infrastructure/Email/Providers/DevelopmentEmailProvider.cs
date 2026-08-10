using Microsoft.Extensions.Logging;
using StudentWorkforceManagement.Application.Common.Email;

namespace StudentWorkforceManagement.Infrastructure.Email.Providers;

public sealed class DevelopmentEmailProvider(ILogger<DevelopmentEmailProvider> logger) : IEmailProvider
{
    public Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Development email sink accepted message {IdempotencyKey} using template {TemplateKey}", message.IdempotencyKey, message.TemplateKey);
        return Task.FromResult(new EmailProviderResult(true, $"dev-{message.IdempotencyKey}", null));
    }
}
