using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using StudentWorkforceManagement.Api.Authentication;
using StudentWorkforceManagement.Api.Security;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Infrastructure.Security.Tokens;

namespace StudentWorkforceManagement.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        ValidateProductionApiConfiguration(configuration, environment);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
        services.AddMemoryCache();
        services.AddSingleton<AuthAttemptLimiter>();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }
            });
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateActor = false,
                    ValidateTokenReplay = false,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = "role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrWhiteSpace(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Principal?.FindFirstValue("sub");
                        var sessionIdValue = context.Principal?.FindFirstValue("sid");
                        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(sessionIdValue, out var sessionId))
                        {
                            context.Fail("Token is missing required user/session claims.");
                            return;
                        }

                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var session = await dbContext.Sessions.AsNoTracking()
                            .Include(item => item.User)
                            .SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, context.HttpContext.RequestAborted);
                        if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= DateTimeOffset.UtcNow || session.User is null || !session.User.IsActive || session.User.DeletedAt.HasValue)
                        {
                            context.Fail("Session is not active.");
                        }
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ADMIN", policy => policy.RequireRole(UserRole.ADMIN.ToString()));
            options.AddPolicy("TASK_MANAGER", policy => policy.RequireRole(UserRole.TASK_MANAGER.ToString()));
            options.AddPolicy("REVIEWER", policy => policy.RequireRole(UserRole.REVIEWER.ToString()));
            options.AddPolicy("STUDENT", policy => policy.RequireRole(UserRole.STUDENT.ToString()));
            options.AddPolicy("STAFF_TASK_MANAGEMENT", policy => policy.RequireRole(UserRole.ADMIN.ToString(), UserRole.TASK_MANAGER.ToString()));
            options.AddPolicy("REVIEWERS", policy => policy.RequireRole(UserRole.ADMIN.ToString(), UserRole.REVIEWER.ToString()));
        });

        services.AddProblemDetails();
        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var schemaType = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
                if (schemaType.IsEnum)
                {
                    schema.Type = "string";
                    schema.Format = null;
                    schema.Enum = Enum.GetNames(schemaType)
                        .Select(name => new OpenApiString(name))
                        .Cast<IOpenApiAny>()
                        .ToList();
                }

                return Task.CompletedTask;
            });
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes["bearerAuth"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Bearer access token"
                };
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
                {
                    return Task.CompletedTask;
                }

                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }] = []
                });
                return Task.CompletedTask;
            });
        });
        return services;
    }

    private static void ValidateProductionApiConfiguration(IConfiguration configuration, IHostEnvironment? environment)
    {
        if (environment?.IsDevelopment() != false)
        {
            return;
        }

        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            throw new InvalidOperationException("Production AllowedHosts must be explicitly configured.");
        }

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 || origins.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Production Cors:AllowedOrigins must include at least one explicit frontend origin.");
        }
    }
}
