using System.ComponentModel.DataAnnotations;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool EnableServer { get; init; } = true;
    public string HangfireSchemaName { get; init; } = "hangfire";

    [Range(1, 500)]
    public int BatchSize { get; init; } = 100;

    [Range(1, 30)]
    public int JobRetryAttempts { get; init; } = 3;

    [Range(1, 1440)]
    public int PendingUploadGraceMinutes { get; init; } = 60;
}
