namespace StudentWorkforceManagement.Application.Common.Email;

public interface IEmailProvider
{
    System.Threading.Tasks.Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
