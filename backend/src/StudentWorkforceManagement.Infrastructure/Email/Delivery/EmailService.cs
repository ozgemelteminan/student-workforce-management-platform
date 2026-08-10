using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Email;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Infrastructure.Email.Delivery;

public sealed class EmailService(ApplicationDbContext dbContext, IEmailSecretProtector secretProtector) : IEmailService
{
    public async System.Threading.Tasks.Task QueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (dbContext.EmailDeliveries.Local.Any(email => email.IdempotencyKey == message.IdempotencyKey) ||
            await dbContext.EmailDeliveries.AnyAsync(email => email.IdempotencyKey == message.IdempotencyKey, cancellationToken))
        {
            return;
        }

        var templateData = new Dictionary<string, string>(message.TemplateData, StringComparer.Ordinal);
        foreach (var secret in message.SecretTemplateData ?? new Dictionary<string, string>())
        {
            if (templateData.ContainsKey(secret.Key))
            {
                throw new InvalidOperationException($"Email template data key '{secret.Key}' cannot be both public and secret.");
            }

            templateData[EmailMessageFactory.ProtectedTemplateDataKey(secret.Key)] = secretProtector.Protect(secret.Value);
        }

        dbContext.EmailDeliveries.Add(new EmailDelivery
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = message.IdempotencyKey,
            RecipientEmail = message.To,
            Subject = message.Subject,
            TemplateKey = message.TemplateKey,
            TemplateDataJson = JsonSerializer.Serialize(templateData),
            Status = EmailDeliveryStatus.QUEUED
        });
    }
}
