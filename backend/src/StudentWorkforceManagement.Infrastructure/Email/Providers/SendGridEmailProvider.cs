using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Email;

namespace StudentWorkforceManagement.Infrastructure.Email.Providers;

public sealed class SendGridEmailProvider(HttpClient httpClient, IOptions<EmailOptions> options, EmailTemplateRenderer renderer) : IEmailProvider
{
    private readonly EmailOptions _options = options.Value;

    public async Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.SendGridApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new EmailProviderResult(false, null, "SendGrid API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            personalizations = new[] { new { to = new[] { new { email = message.To } } } },
            from = new { email = _options.FromEmail, name = _options.FromName },
            subject = message.Subject,
            content = new[] { new { type = "text/html", value = renderer.RenderHtml(message) } },
            custom_args = new Dictionary<string, string> { ["idempotency_key"] = message.IdempotencyKey }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return new EmailProviderResult(true, response.Headers.TryGetValues("X-Message-Id", out var values) ? values.FirstOrDefault() : message.IdempotencyKey, null);
        }

        return new EmailProviderResult(false, null, $"SendGrid rejected message with status {(int)response.StatusCode}.");
    }
}
