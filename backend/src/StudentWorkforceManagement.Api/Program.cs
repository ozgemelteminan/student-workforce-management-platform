using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using StudentWorkforceManagement.Api;
using StudentWorkforceManagement.Api.Endpoints;
using StudentWorkforceManagement.Api.Middleware;
using StudentWorkforceManagement.Application;
using StudentWorkforceManagement.Infrastructure;
using StudentWorkforceManagement.Infrastructure.Hosting;
using StudentWorkforceManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Student Workforce Management API v1";
        options.SwaggerEndpoint("/openapi/v1.json", "Student Workforce Management API v1");
    });
    await DevelopmentAdminSeeder.SeedAsync(app.Services, app.Environment, app.Configuration);
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapInfrastructureEndpoints();
app.MapV1Api();

app.Run();

public partial class Program;
