namespace StudentWorkforceManagement.Application.Common.Email;

public interface IEmailProvider
{
    Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
