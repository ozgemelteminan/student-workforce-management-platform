using MediatR;

namespace StudentWorkforceManagement.Application.Common.Events;

public interface IApplicationEventQueue
{
    void Enqueue(INotification notification);
    System.Threading.Tasks.Task PublishQueuedAsync(CancellationToken cancellationToken = default);
}

public sealed class ApplicationEventQueue(IPublisher publisher) : IApplicationEventQueue
{
    private readonly Queue<INotification> queuedNotifications = new();

    public void Enqueue(INotification notification)
    {
        queuedNotifications.Enqueue(notification);
    }

    public async Task PublishQueuedAsync(CancellationToken cancellationToken = default)
    {
        while (queuedNotifications.Count > 0)
        {
            await publisher.Publish(queuedNotifications.Dequeue(), cancellationToken);
        }
    }
}
