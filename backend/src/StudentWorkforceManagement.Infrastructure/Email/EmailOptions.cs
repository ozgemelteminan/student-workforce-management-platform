using System.ComponentModel.DataAnnotations;

namespace StudentWorkforceManagement.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string Provider { get; init; } = "Development";

    [Range(1, 20)]
    public int BatchSize { get; init; } = 25;

    [Range(1, 10)]
    public int MaxAttempts { get; init; } = 5;

    [Range(1, 1440)]
    public int BaseRetryDelayMinutes { get; init; } = 5;

    [Range(1, 120)]
    public int ProviderTimeoutSeconds { get; init; } = 15;

    [EmailAddress]
    public string FromEmail { get; init; } = "no-reply@example.invalid";

    public string FromName { get; init; } = "Student Workforce Management";
    public string DevelopmentSinkPath { get; init; } = "storage/dev-emails";
    public string DevelopmentFrontendBaseUrl { get; init; } = "http://localhost:5173";
    public string? SendGridApiKey { get; init; }
    public SmtpOptions Smtp { get; init; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string? Username { get; init; }
    public string? Password { get; init; }
}
