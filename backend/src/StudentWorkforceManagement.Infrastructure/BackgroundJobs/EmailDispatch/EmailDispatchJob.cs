using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Email;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Email;
using StudentWorkforceManagement.Infrastructure.Email.Delivery;
using StudentWorkforceManagement.Infrastructure.Persistence;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.EmailDispatch;

public sealed class EmailDispatchJob(
    ApplicationDbContext dbContext,
    IEmailProvider emailProvider,
    EmailMessageFactory messageFactory,
    IUtcClock clock,
    IOptions<EmailOptions> options,
    ILogger<EmailDispatchJob> logger)
{
    private readonly EmailOptions _options = options.Value;

    [AutomaticRetry(Attempts = 3)]
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var deliveries = await dbContext.EmailDeliveries
            .Where(item =>
                (item.Status == EmailDeliveryStatus.QUEUED || item.Status == EmailDeliveryStatus.FAILED) &&
                item.Attempts < _options.MaxAttempts &&
                (item.NextRetryAt == null || item.NextRetryAt <= now))
            .OrderBy(item => item.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var delivery in deliveries)
        {
            delivery.Status = EmailDeliveryStatus.PROCESSING;
            delivery.Attempts += 1;
            delivery.LastAttemptAt = now;
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var sent = 0;
        foreach (var delivery in deliveries)
        {
            EmailProviderResult result;
            try
            {
                var message = messageFactory.FromDelivery(delivery);
                result = await emailProvider.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Email provider failed for delivery {DeliveryId}", delivery.Id);
                result = new EmailProviderResult(false, null, "Email delivery failed.");
            }

            if (result.Accepted)
            {
                delivery.Status = EmailDeliveryStatus.SENT;
                delivery.ProviderName = emailProvider.GetType().Name;
                delivery.ProviderMessageId = result.ProviderMessageId;
                delivery.FailureReason = null;
                delivery.SentAt = clock.UtcNow;
                delivery.NextRetryAt = null;
                delivery.TemplateDataJson = EmailMessageFactory.RemoveProtectedTemplateData(delivery.TemplateDataJson);
                sent += 1;
            }
            else
            {
                delivery.Status = delivery.Attempts >= _options.MaxAttempts ? EmailDeliveryStatus.FAILED : EmailDeliveryStatus.QUEUED;
                delivery.FailureReason = result.FailureReason;
                delivery.NextRetryAt = delivery.Attempts >= _options.MaxAttempts ? null : clock.UtcNow.AddMinutes(_options.BaseRetryDelayMinutes * Math.Pow(2, delivery.Attempts - 1));
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("EmailDispatchJob processed {Count} deliveries and sent {Sent}", deliveries.Count, sent);
        return sent;
    }
}
