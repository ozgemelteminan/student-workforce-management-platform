namespace StudentWorkforceManagement.Application.Common.Email;

public interface IEmailService
{
    Task QueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
