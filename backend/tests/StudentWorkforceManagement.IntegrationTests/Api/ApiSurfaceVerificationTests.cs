namespace StudentWorkforceManagement.IntegrationTests.Api;

public sealed class ApiSurfaceVerificationTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Program_wires_authentication_authorization_and_v1_endpoints()
    {
        var program = Read("backend/src/StudentWorkforceManagement.Api/Program.cs");

        Assert.Contains("builder.Services.AddApi(builder.Configuration);", program);
        Assert.Contains("app.UseMiddleware<ExceptionHandlingMiddleware>();", program);
        Assert.Contains("app.UseMiddleware<SecurityHeadersMiddleware>();", program);
        Assert.Contains("app.UseAuthentication();", program);
        Assert.Contains("app.UseAuthorization();", program);
        Assert.Contains("app.MapInfrastructureEndpoints();", program);
        Assert.Contains("app.MapV1Api();", program);
        Assert.True(program.IndexOf("app.UseAuthentication();", StringComparison.Ordinal) < program.IndexOf("app.MapV1Api();", StringComparison.Ordinal));
    }

    [Fact]
    public void Api_dependency_injection_defines_jwt_signalr_policies_and_current_user()
    {
        var dependencyInjection = Read("backend/src/StudentWorkforceManagement.Api/DependencyInjection.cs");

        Assert.Contains("AddJwtBearer", dependencyInjection);
        Assert.Contains("StartsWithSegments(\"/hubs/notifications\")", dependencyInjection);
        Assert.Contains("ICurrentUserService, HttpCurrentUserService", dependencyInjection);
        Assert.Contains("options.AddPolicy(\"ADMIN\"", dependencyInjection);
        Assert.Contains("options.AddPolicy(\"STAFF_TASK_MANAGEMENT\"", dependencyInjection);
        Assert.Contains("options.AddPolicy(\"REVIEWERS\"", dependencyInjection);
        Assert.DoesNotContain("AllowAnyOrigin", dependencyInjection);
    }

    [Fact]
    public void V1_endpoint_groups_are_protected_and_do_not_use_fake_authentication()
    {
        var endpoints = Read("backend/src/StudentWorkforceManagement.Api/Endpoints/V1Endpoints.cs");

        Assert.Contains("api.MapGroup(\"/auth\")", endpoints);
        Assert.Contains("api.MapGroup(\"/tasks\").RequireAuthorization()", endpoints);
        Assert.Contains("api.MapGroup(\"/students\").RequireAuthorization()", endpoints);
        Assert.Contains("api.MapGroup(\"/files\").RequireAuthorization()", endpoints);
        Assert.Contains("api.MapGroup(\"/analytics\").RequireAuthorization(\"STAFF_TASK_MANAGEMENT\")", endpoints);
        Assert.Contains("api.MapGroup(\"/audit\").RequireAuthorization(\"ADMIN\")", endpoints);
        Assert.Contains("RequireAuthorization(\"STUDENT\")", endpoints);
        Assert.Contains("RequireAuthorization(\"REVIEWERS\")", endpoints);
        Assert.False(endpoints.Contains("Fake", StringComparison.OrdinalIgnoreCase));
        Assert.False(endpoints.Contains("TestAuth", StringComparison.OrdinalIgnoreCase));
        Assert.False(endpoints.Contains("TODO", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Hangfire_dashboard_and_signalr_hub_require_authenticated_users()
    {
        var infrastructureEndpoints = Read("backend/src/StudentWorkforceManagement.Infrastructure/Hosting/InfrastructureEndpointRouteBuilderExtensions.cs");
        var hub = Read("backend/src/StudentWorkforceManagement.Infrastructure/Notifications/SignalR/NotificationHub.cs");

        Assert.Contains("MapHub<NotificationHub>(\"/hubs/notifications\")", infrastructureEndpoints);
        Assert.Contains("MapHangfireDashboard(\"/admin/hangfire\"", infrastructureEndpoints);
        Assert.Contains("UserRole.ADMIN", infrastructureEndpoints);
        Assert.Contains("[Authorize]", hub);
    }

    private static string Read(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "StudentWorkforceManagement.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
