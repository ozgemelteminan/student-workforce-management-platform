using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Notifications.SignalR;

public interface INotificationRealtimeDispatcher
{
    System.Threading.Tasks.Task DispatchAsync(Notification notification, CancellationToken cancellationToken = default);
}

public sealed class SignalRNotificationDispatcher(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationDispatcher> logger) : INotificationRealtimeDispatcher
{
    public async System.Threading.Tasks.Task DispatchAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Dispatching notification {NotificationId} to user {UserId}", notification.Id, notification.UserId);
        await hubContext.Clients.User(notification.UserId.ToString("D")).SendAsync("NotificationCreated", new
        {
            EventType = "notification.created",
            NotificationId = notification.Id,
            NotificationType = notification.Type.ToString(),
            notification.RelatedEntityType,
            notification.RelatedEntityId,
            notification.CreatedAt
        }, cancellationToken);
    }
}
