using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Persistence.Interceptors;
using StudentWorkforceManagement.Infrastructure.Security.Password;
using StudentWorkforceManagement.Infrastructure.Time;

namespace StudentWorkforceManagement.Infrastructure.Persistence;

public static class ProductionSmokeTestStudentSeeder
{
    public const string CommandName = "--seed-production-smoke-test-student";
    public const string Email = "test.student@local.test";
    public const string Password = "TestStudent123";

    public static bool ShouldRun(string[] args) => args.Any(argument => string.Equals(argument, CommandName, StringComparison.OrdinalIgnoreCase));

    public static async System.Threading.Tasks.Task RunCliAsync(IConfiguration configuration, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (!environment.IsProduction())
        {
            throw new InvalidOperationException("The production smoke-test student seed can only run when ASPNETCORE_ENVIRONMENT=Production.");
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Production smoke-test student seed requires DATABASE_CONNECTION_STRING.");
        }

        var services = new ServiceCollection();
        AddSeedServices(services, connectionString);
        await using var provider = services.BuildServiceProvider();
        await SeedAsync(provider, environment, cancellationToken);
    }

    public static IServiceCollection AddSeedServices(IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Production smoke-test student seed requires a PostgreSQL connection string.");
        }

        services.AddLogging();
        services.AddSingleton<IUtcClock, UtcClock>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<ConcurrencyTokenInterceptor>();
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<ConcurrencyTokenInterceptor>());
        });
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordService, IdentityPasswordService>();
        return services;
    }

    public static async System.Threading.Tasks.Task SeedAsync(IServiceProvider services, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (!environment.IsProduction())
        {
            throw new InvalidOperationException("The production smoke-test student seed can only run when ASPNETCORE_ENVIRONMENT=Production.");
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ProductionSmokeTestStudentSeeder));

        var existingUser = await dbContext.Users.IgnoreQueryFilters().SingleOrDefaultAsync(user => user.Email == Email, cancellationToken);
        if (existingUser is not null)
        {
            logger.LogInformation("Production smoke-test STUDENT account {Email} already exists; no changes applied.", Email);
            return;
        }

        var studentRole = await dbContext.Roles.SingleOrDefaultAsync(role => role.Name == UserRole.STUDENT, cancellationToken);
        if (studentRole is null)
        {
            studentRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = UserRole.STUDENT,
                Description = "Student user."
            };
            dbContext.Roles.Add(studentRole);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = Email,
            DisplayName = "Test Student",
            IsActive = true,
            RoleId = studentRole.Id,
            Role = studentRole
        };
        user.PasswordHash = passwordService.HashPassword(user, Password);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            FirstName = "Test",
            LastName = "Student",
            Email = Email,
            Department = "Computer Engineering",
            WeeklyTargetMinutes = 600,
            IsActive = true
        };

        dbContext.Users.Add(user);
        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created explicit production smoke-test STUDENT account {Email}.", Email);
    }
}
