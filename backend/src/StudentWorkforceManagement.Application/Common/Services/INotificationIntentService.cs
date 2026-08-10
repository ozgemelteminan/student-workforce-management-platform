using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Common.Services;

public interface INotificationIntentService
{
    System.Threading.Tasks.Task CreateAsync(Guid userId, NotificationType type, string title, string message, string? relatedEntityType, Guid? relatedEntityId, string? idempotencyKey = null, CancellationToken cancellationToken = default);
}
