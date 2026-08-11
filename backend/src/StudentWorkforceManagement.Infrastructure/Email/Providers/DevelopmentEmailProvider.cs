using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Email;
using StudentWorkforceManagement.Infrastructure.Email;

namespace StudentWorkforceManagement.Infrastructure.Email.Providers;

public sealed class DevelopmentEmailProvider(
    IHostEnvironment environment,
    IOptions<EmailOptions> options,
    EmailTemplateRenderer renderer,
    ILogger<DevelopmentEmailProvider> logger) : IEmailProvider
{
    private readonly EmailOptions _options = options.Value;

    public async Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (environment.IsDevelopment() == false)
        {
            throw new InvalidOperationException("Development email provider can only be used in the Development environment.");
        }

        await WriteDevelopmentSinkAsync(message, cancellationToken);
        logger.LogInformation("Development email sink accepted message {IdempotencyKey} using template {TemplateKey}", message.IdempotencyKey, message.TemplateKey);
        return new EmailProviderResult(true, $"dev-{message.IdempotencyKey}", null);
    }

    private async Task WriteDevelopmentSinkAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.DevelopmentSinkPath))
        {
            return;
        }

        Directory.CreateDirectory(_options.DevelopmentSinkPath);
        var html = renderer.RenderHtml(message);
        var resetUrl = TryCreateResetUrl(message);
        var payload = new DevelopmentEmailSinkMessage(
            DateTimeOffset.UtcNow,
            message.To,
            message.Subject,
            message.TemplateKey,
            message.IdempotencyKey,
            message.TemplateData,
            message.SecretTemplateData ?? new Dictionary<string, string>(),
            resetUrl,
            html);

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        var templateFile = Path.Combine(_options.DevelopmentSinkPath, $"latest-{SafeFileName(message.TemplateKey)}.json");
        var latestFile = Path.Combine(_options.DevelopmentSinkPath, "latest.json");
        var htmlFile = Path.Combine(_options.DevelopmentSinkPath, $"latest-{SafeFileName(message.TemplateKey)}.html");

        await File.WriteAllTextAsync(templateFile, json, cancellationToken);
        await File.WriteAllTextAsync(latestFile, json, cancellationToken);
        await File.WriteAllTextAsync(htmlFile, html, cancellationToken);
    }

    private string? TryCreateResetUrl(EmailMessage message)
    {
        if (message.TemplateKey.Equals("auth.password-reset", StringComparison.Ordinal) == false ||
            message.SecretTemplateData?.TryGetValue("resetToken", out var resetToken) != true ||
            string.IsNullOrWhiteSpace(resetToken))
        {
            return null;
        }

        var baseUrl = string.IsNullOrWhiteSpace(_options.DevelopmentFrontendBaseUrl)
            ? "http://localhost:5173"
            : _options.DevelopmentFrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/reset-password?token={UrlEncoder.Default.Encode(resetToken)}";
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(character => invalid.Contains(character) || character is '/' or '\\' ? '-' : character).ToArray();
        return new string(chars);
    }

    private sealed record DevelopmentEmailSinkMessage(
        DateTimeOffset CapturedAt,
        string To,
        string Subject,
        string TemplateKey,
        string IdempotencyKey,
        IReadOnlyDictionary<string, string> TemplateData,
        IReadOnlyDictionary<string, string> SecretTemplateData,
        string? ResetUrl,
        string Html);
}
