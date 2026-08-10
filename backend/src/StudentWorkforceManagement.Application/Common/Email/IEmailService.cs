namespace StudentWorkforceManagement.Application.Common.Email;

public interface IEmailService
{
    System.Threading.Tasks.Task QueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
