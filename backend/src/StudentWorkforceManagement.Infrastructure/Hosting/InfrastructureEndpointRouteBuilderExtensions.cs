using Microsoft.AspNetCore.Builder;
using Hangfire;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.Notifications.SignalR;

namespace StudentWorkforceManagement.Infrastructure.Hosting;

public static class InfrastructureEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapInfrastructureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/hubs/notifications");
        JobScheduleRegistrar.RegisterRecurringJobs(endpoints.ServiceProvider.GetRequiredService<IRecurringJobManager>());
        return endpoints;
    }
}
