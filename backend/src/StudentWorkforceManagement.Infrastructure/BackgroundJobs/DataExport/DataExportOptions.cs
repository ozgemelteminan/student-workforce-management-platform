using System.ComponentModel.DataAnnotations;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.DataExport;

public sealed class DataExportOptions
{
    public const string SectionName = "Exports";

    [Range(1, 168)]
    public int ArtifactExpirationHours { get; init; } = 24;
}
