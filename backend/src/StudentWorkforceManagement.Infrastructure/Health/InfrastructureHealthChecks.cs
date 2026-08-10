using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Infrastructure.Storage;

namespace StudentWorkforceManagement.Infrastructure.Health;

public sealed class PostgreSqlHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
            : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
    }
}

public sealed class RedisHealthCheck(IConnectionMultiplexer? redis = null) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (redis is null)
        {
            return Task.FromResult(HealthCheckResult.Degraded("Redis is not configured."));
        }
        return Task.FromResult(redis.IsConnected ? HealthCheckResult.Healthy("Redis is connected.") : HealthCheckResult.Unhealthy("Redis is disconnected."));
    }
}

public sealed class StorageHealthCheck(IFileStorage storage, IOptions<StorageOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _ = storage;
        var provider = options.Value.Provider;
        return string.IsNullOrWhiteSpace(provider)
            ? Task.FromResult(HealthCheckResult.Unhealthy("Storage provider is not configured."))
            : Task.FromResult(HealthCheckResult.Healthy($"Storage provider {provider} is configured."));
    }
}

public sealed class HangfireHealthCheck(JobStorage storage) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            storage.GetMonitoringApi().GetStatistics();
            return Task.FromResult(HealthCheckResult.Healthy("Hangfire storage is reachable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage is not reachable.", ex));
        }
    }
}
