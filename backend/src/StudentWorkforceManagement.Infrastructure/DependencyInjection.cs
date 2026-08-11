using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Application.Common.Email;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Files.Services;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.DataExport;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.DeadlineReminders;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.EmailDispatch;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.MarketplaceClaimExpiration;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.OrphanFileCleanup;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.OverdueTasks;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.RecurringTasks;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.RetentionCleanup;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.SemesterRollover;
using StudentWorkforceManagement.Infrastructure.Email;
using StudentWorkforceManagement.Infrastructure.Email.Delivery;
using StudentWorkforceManagement.Infrastructure.Email.Providers;
using StudentWorkforceManagement.Infrastructure.Health;
using StudentWorkforceManagement.Infrastructure.Notifications.SignalR;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Infrastructure.Persistence.Interceptors;
using StudentWorkforceManagement.Infrastructure.Security.Password;
using StudentWorkforceManagement.Infrastructure.Security.Tokens;
using StudentWorkforceManagement.Infrastructure.Storage;
using StudentWorkforceManagement.Infrastructure.Storage.Local;
using StudentWorkforceManagement.Infrastructure.Storage.ObjectStorage;
using StudentWorkforceManagement.Infrastructure.Time;

namespace StudentWorkforceManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IUtcClock, UtcClock>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<ConcurrencyTokenInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_CONNECTION_STRING"]
            ?? "Host=localhost;Port=5432;Database=student_workforce;Username=student_workforce;Password=student_workforce_dev_password";

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<ConcurrencyTokenInterceptor>());
        });
        services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.SigningKey.Length >= 32, "JWT signing key must be at least 32 characters.")
            .Validate(options => options.SigningKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) == false, "JWT signing key must be supplied by secure environment configuration.")
            .ValidateOnStart();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordService, IdentityPasswordService>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddScoped<IAccessTokenService, JwtAccessTokenService>();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(ValidateEmailOptions, "Invalid email provider configuration.")
            .ValidateOnStart();
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("StudentWorkforceManagement");
        var dataProtectionKeysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(dataProtectionKeysPath) == false)
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
        }
        services.AddSingleton<IEmailSecretProtector, DataProtectionEmailSecretProtector>();
        services.AddSingleton<EmailTemplateRenderer>();
        services.AddScoped<EmailMessageFactory>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<DevelopmentEmailProvider>();
        services.AddScoped<SmtpEmailProvider>();
        services.AddHttpClient<SendGridEmailProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.sendgrid.com/v3/");
            client.Timeout = TimeSpan.FromSeconds(15);
        }).AddStandardResilienceHandler();
        services.AddScoped<IEmailProvider>(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value.Provider;
            return provider.Trim().ToUpperInvariant() switch
            {
                "DEVELOPMENT" => serviceProvider.GetRequiredService<DevelopmentEmailProvider>(),
                "SMTP" => serviceProvider.GetRequiredService<SmtpEmailProvider>(),
                "SENDGRID" => serviceProvider.GetRequiredService<SendGridEmailProvider>(),
                _ => throw new InvalidOperationException($"Unsupported email provider '{provider}'.")
            };
        });

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(ValidateStorageOptions, "Invalid storage provider configuration.")
            .ValidateOnStart();
        services.AddOptions<UploadFilePolicyOptions>()
            .Bind(configuration.GetSection(UploadFilePolicyOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(UploadFilePolicy.ValidateOptions, "Invalid upload file policy configuration.")
            .ValidateOnStart();
        services.AddScoped<LocalFileStorage>();
        services.AddScoped<S3FileStorage>();
        services.AddScoped<IFileStorage>(serviceProvider =>
        {
            var provider = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value.Provider;
            return provider.Trim().ToUpperInvariant() switch
            {
                "LOCAL" => serviceProvider.GetRequiredService<LocalFileStorage>(),
                "S3" => serviceProvider.GetRequiredService<S3FileStorage>(),
                _ => throw new InvalidOperationException($"Unsupported storage provider '{provider}'.")
            };
        });

        services.AddOptions<BackgroundJobOptions>()
            .Bind(configuration.GetSection(BackgroundJobOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var backgroundJobOptions = configuration.GetSection(BackgroundJobOptions.SectionName).Get<BackgroundJobOptions>() ?? new BackgroundJobOptions();
        services.AddHangfire(config => config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                SchemaName = backgroundJobOptions.HangfireSchemaName,
                PrepareSchemaIfNecessary = true
            }));
        if (backgroundJobOptions.EnableServer)
        {
            services.AddHangfireServer(options => options.WorkerCount = Math.Max(1, Environment.ProcessorCount));
        }

        services.AddScoped<DeadlineReminderJob>();
        services.AddScoped<OverdueTaskJob>();
        services.AddScoped<IRecurringScheduleCalculator, RecurringScheduleCalculator>();
        services.AddScoped<RecurringTaskJob>();
        services.AddScoped<EmailDispatchJob>();
        services.AddScoped<OrphanFileCleanupJob>();
        services.AddScoped<RetentionCleanupJob>();
        services.AddScoped<DataExportJob>();
        services.AddScoped<IExportJobScheduler, HangfireExportJobScheduler>();
        services.AddOptions<DataExportOptions>()
            .Bind(configuration.GetSection(DataExportOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<MarketplaceClaimExpirationJob>();
        services.AddScoped<SemesterRolloverJob>();

        var redisConnection = configuration["REDIS_CONNECTION_STRING"] ?? configuration.GetConnectionString("Redis");
        var signalR = services.AddSignalR();
        if (string.IsNullOrWhiteSpace(redisConnection) == false)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
            signalR.AddStackExchangeRedis(redisConnection);
        }
        services.AddScoped<INotificationRealtimeDispatcher, SignalRNotificationDispatcher>();

        services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>("postgresql", HealthStatus.Unhealthy, ["ready"])
            .AddCheck<StorageHealthCheck>("storage", HealthStatus.Unhealthy, ["ready"])
            .AddCheck<HangfireHealthCheck>("hangfire", HealthStatus.Unhealthy, ["ready"])
            .AddCheck<RedisHealthCheck>("redis", HealthStatus.Degraded, ["ready"]);

        return services;
    }

    private static bool ValidateEmailOptions(EmailOptions options)
    {
        var provider = options.Provider.Trim().ToUpperInvariant();
        return provider switch
        {
            "DEVELOPMENT" => true,
            "SMTP" => string.IsNullOrWhiteSpace(options.Smtp.Host) == false,
            "SENDGRID" => string.IsNullOrWhiteSpace(options.SendGridApiKey) == false,
            _ => false
        };
    }

    private static bool ValidateStorageOptions(StorageOptions options)
    {
        if (options.SignedUrlLifetimeMinutes > 60)
        {
            return false;
        }
        var provider = options.Provider.Trim().ToUpperInvariant();
        return provider switch
        {
            "LOCAL" => string.IsNullOrWhiteSpace(options.LocalRootPath) == false,
            "S3" => string.IsNullOrWhiteSpace(options.S3.BucketName) == false && string.IsNullOrWhiteSpace(options.S3.AccessKey) == false && string.IsNullOrWhiteSpace(options.S3.SecretKey) == false,
            _ => false
        };
    }
}
