using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Infrastructure.Persistence.Interceptors;
using StudentWorkforceManagement.Infrastructure.Time;

namespace StudentWorkforceManagement.Infrastructure.Persistence;

public static class ProductionReferenceDataSeeder
{
    public const string CommandName = "--seed-production-reference-data";

    public static readonly IReadOnlyCollection<string> CategoryNames =
    [
        "Administrative",
        "Academic Support",
        "Technical / IT",
        "Content & Communication",
        "Event Support",
        "Data & Reporting"
    ];

    public static readonly IReadOnlyCollection<string> SkillNames =
    [
        "Microsoft Excel",
        "Microsoft Word",
        "Microsoft PowerPoint",
        "Canva",
        "Data Entry",
        "Data Analysis",
        "Research",
        "Documentation",
        "Content Writing",
        "Communication",
        "Social Media",
        "Event Support",
        "Event Coordination",
        "Technical Support",
        "Programming",
        "Web Development",
        "Git / GitHub",
        "Graphic Design"
    ];

    public static bool ShouldRun(string[] args) => args.Any(argument => string.Equals(argument, CommandName, StringComparison.OrdinalIgnoreCase));

    public static async System.Threading.Tasks.Task RunCliAsync(IConfiguration configuration, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (!environment.IsProduction())
        {
            throw new InvalidOperationException("The production reference-data seed can only run when ASPNETCORE_ENVIRONMENT=Production.");
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? configuration["DATABASE_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Production reference-data seed requires DATABASE_CONNECTION_STRING.");
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
            throw new InvalidOperationException("Production reference-data seed requires a PostgreSQL connection string.");
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
        return services;
    }

    public static async System.Threading.Tasks.Task SeedAsync(IServiceProvider services, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (!environment.IsProduction())
        {
            throw new InvalidOperationException("The production reference-data seed can only run when ASPNETCORE_ENVIRONMENT=Production.");
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ProductionReferenceDataSeeder));

        var existingCategories = await dbContext.Categories.ToListAsync(cancellationToken);
        foreach (var name in CategoryNames)
        {
            if (existingCategories.Any(category => SameName(category.Name, name)))
            {
                continue;
            }

            var category = new Category { Id = Guid.NewGuid(), Name = name.Trim(), IsActive = true };
            dbContext.Categories.Add(category);
            existingCategories.Add(category);
        }

        var existingSkills = await dbContext.Skills.ToListAsync(cancellationToken);
        foreach (var name in SkillNames)
        {
            if (existingSkills.Any(skill => SameName(skill.Name, name)))
            {
                continue;
            }

            var skill = new Skill { Id = Guid.NewGuid(), Name = name.Trim(), IsActive = true };
            dbContext.Skills.Add(skill);
            existingSkills.Add(skill);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Production reference data seed completed for {CategoryCount} categories and {SkillCount} skills.", CategoryNames.Count, SkillNames.Count);
    }

    private static bool SameName(string left, string right) => string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
