using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Email;

namespace StudentWorkforceManagement.Infrastructure.Email.Providers;

public sealed class SmtpEmailProvider(IOptions<EmailOptions> options, EmailTemplateRenderer renderer) : IEmailProvider
{
    private readonly EmailOptions _options = options.Value;

    public async Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var smtp = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.EnableSsl,
            Timeout = _options.ProviderTimeoutSeconds * 1000
        };
        if (!string.IsNullOrWhiteSpace(_options.Smtp.Username))
        {
            smtp.Credentials = new NetworkCredential(_options.Smtp.Username, _options.Smtp.Password);
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = message.Subject,
            Body = renderer.RenderHtml(message),
            IsBodyHtml = true
        };
        mail.To.Add(message.To);
        await smtp.SendMailAsync(mail, cancellationToken).WaitAsync(TimeSpan.FromSeconds(_options.ProviderTimeoutSeconds), cancellationToken);
        return new EmailProviderResult(true, message.IdempotencyKey, null);
    }
}
