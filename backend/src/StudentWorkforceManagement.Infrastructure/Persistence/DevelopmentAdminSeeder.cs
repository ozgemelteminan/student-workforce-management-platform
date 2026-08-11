using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Infrastructure.Persistence;

public static class DevelopmentAdminSeeder
{
    public const string SectionName = "DevelopmentAdmin";

    public static async System.Threading.Tasks.Task SeedAsync(IServiceProvider services, IHostEnvironment environment, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var options = configuration.GetSection(SectionName).Get<DevelopmentAdminOptions>() ?? new DevelopmentAdminOptions();
        if (!options.Enabled)
        {
            return;
        }

        if (!IsValidDevelopmentPassword(options.Password))
        {
            throw new InvalidOperationException("Development admin seed password must be 8-256 characters and include uppercase, lowercase, and number characters.");
        }

        var normalizedEmail = NormalizeEmail(options.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new InvalidOperationException("Development admin seed email must be configured.");
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevelopmentAdminSeeder));

        var adminRole = await dbContext.Roles.SingleOrDefaultAsync(role => role.Name == UserRole.ADMIN, cancellationToken);
        if (adminRole is null)
        {
            adminRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = UserRole.ADMIN,
                Description = "Full system administration."
            };
            dbContext.Roles.Add(adminRole);
        }

        var user = await dbContext.Users.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? "Development Admin" : options.DisplayName.Trim(),
                IsActive = true,
                RoleId = adminRole.Id,
                Role = adminRole
            };
            user.PasswordHash = passwordService.HashPassword(user, options.Password);
            dbContext.Users.Add(user);
            logger.LogInformation("Created Development-only disposable ADMIN account {Email}.", normalizedEmail);
        }
        else
        {
            user.RoleId = adminRole.Id;
            user.Role = adminRole;
            user.IsActive = true;
            user.DeletedAt = null;
            if (string.IsNullOrWhiteSpace(user.DisplayName))
            {
                user.DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? "Development Admin" : options.DisplayName.Trim();
            }
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = passwordService.HashPassword(user, options.Password);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsValidDevelopmentPassword(string password)
    {
        return password.Length is >= 8 and <= 256
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit);
    }
}

public sealed class DevelopmentAdminOptions
{
    public bool Enabled { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = "Development Admin";
    public string Password { get; init; } = string.Empty;
}
