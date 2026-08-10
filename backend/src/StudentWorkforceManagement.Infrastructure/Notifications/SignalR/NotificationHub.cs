using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace StudentWorkforceManagement.Infrastructure.Notifications.SignalR;

[Authorize]
public sealed class NotificationHub : Hub
{
}
