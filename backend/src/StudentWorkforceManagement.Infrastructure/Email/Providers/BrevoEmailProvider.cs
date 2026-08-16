using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Email;

namespace StudentWorkforceManagement.Infrastructure.Email.Providers;

public sealed class BrevoEmailProvider(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    EmailTemplateRenderer renderer) : IEmailProvider
{
    private readonly EmailOptions _options = options.Value;

    public async Task<EmailProviderResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _options.Brevo.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new EmailProviderResult(
                false,
                null,
                "Brevo API key is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "v3/smtp/email");

        request.Headers.Add("api-key", apiKey);
        request.Headers.Add("accept", "application/json");

        request.Content = JsonContent.Create(new
        {
            sender = new
            {
                email = _options.FromEmail,
                name = _options.FromName
            },
            to = new[]
            {
                new
                {
                    email = message.To
                }
            },
            subject = message.Subject,
            htmlContent = renderer.RenderHtml(message)
        });

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            string? messageId = null;

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    using var json = JsonDocument.Parse(responseBody);

                    if (json.RootElement.TryGetProperty(
                            "messageId",
                            out var messageIdElement))
                    {
                        messageId = messageIdElement.GetString();
                    }
                }
                catch (JsonException)
                {
                    // Successful provider response with an unexpected body.
                }
            }

            return new EmailProviderResult(
                true,
                messageId ?? message.IdempotencyKey,
                null);
        }

        return new EmailProviderResult(
            false,
            null,
            $"Brevo rejected message with status {(int)response.StatusCode}.");
    }
}