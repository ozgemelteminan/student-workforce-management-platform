using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using StudentWorkforceManagement.Infrastructure;
using StudentWorkforceManagement.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace StudentWorkforceManagement.IntegrationTests.Infrastructure;

public sealed class InfrastructureContainerTests
{
    [Fact]
    public async System.Threading.Tasks.Task PostgreSql_container_applies_migrations_and_initializes_hangfire_schema()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .Build();
        await postgres.StartAsync();

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using (var context = new ApplicationDbContext(dbOptions))
        {
            await context.Database.MigrateAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_CONNECTION_STRING"] = postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:SigningKey"] = "TEST_ONLY_SIGNING_KEY_0123456789_32_CHARS",
                ["Email:Provider"] = "Development",
                ["Storage:Provider"] = "Local",
                ["Storage:LocalRootPath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                ["BackgroundJobs:EnableServer"] = "false",
                ["BackgroundJobs:HangfireSchemaName"] = "hangfire"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<JobStorage>();
        storage.GetMonitoringApi().GetStatistics();

        await using var connection = new NpgsqlConnection(postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("select exists (select 1 from information_schema.schemata where schema_name = 'hangfire')", connection);
        var exists = (bool)(await command.ExecuteScalarAsync() ?? false);

        Assert.True(exists);
    }

    [Fact]
    public async System.Threading.Tasks.Task Redis_container_accepts_connections_for_optional_backplane()
    {
        await using var redis = new RedisBuilder("redis:7-alpine")
            .Build();
        await redis.StartAsync();

        await using var connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

        Assert.True(connection.IsConnected);
    }
}
