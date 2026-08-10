using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using StudentWorkforceManagement.Application;
using StudentWorkforceManagement.Infrastructure;
using StudentWorkforceManagement.Infrastructure.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapInfrastructureEndpoints();
app.MapGet("/api/v1", () => Results.Ok(new { version = "v1", status = "foundation" }))
    .WithName("ApiV1Foundation");

app.Run();

public partial class Program;
