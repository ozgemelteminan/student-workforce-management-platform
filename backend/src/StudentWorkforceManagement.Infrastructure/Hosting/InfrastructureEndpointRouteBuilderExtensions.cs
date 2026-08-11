using Microsoft.AspNetCore.Builder;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.Notifications.SignalR;

namespace StudentWorkforceManagement.Infrastructure.Hosting;

public static class InfrastructureEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapInfrastructureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/hubs/notifications");
        endpoints.MapHangfireDashboard("/admin/hangfire", new DashboardOptions
        {
            Authorization = [new AdminHangfireDashboardAuthorizationFilter()]
        });
        if (endpoints.ServiceProvider.GetRequiredService<IOptions<BackgroundJobOptions>>().Value.EnableServer)
        {
            JobScheduleRegistrar.RegisterRecurringJobs(endpoints.ServiceProvider.GetRequiredService<IRecurringJobManager>());
        }
        return endpoints;
    }

    private sealed class AdminHangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var user = context.GetHttpContext().User;
            return user.Identity?.IsAuthenticated == true && user.IsInRole(UserRole.ADMIN.ToString());
        }
    }
}
