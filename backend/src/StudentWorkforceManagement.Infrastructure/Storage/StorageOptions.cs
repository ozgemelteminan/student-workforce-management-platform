using System.ComponentModel.DataAnnotations;

namespace StudentWorkforceManagement.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public const long OneGigabyteInBytes = 1_073_741_824;

    [Required]
    public string Provider { get; init; } = "Local";

    [Range(1, OneGigabyteInBytes)]
    public long MaxFileSizeBytes { get; init; } = OneGigabyteInBytes;

    [Range(1, long.MaxValue)]
    public long StudentStorageQuotaBytes { get; init; } = 10L * OneGigabyteInBytes;

    [Range(1, long.MaxValue)]
    public long DepartmentStorageQuotaBytes { get; init; } = 100L * OneGigabyteInBytes;

    [Range(1, 60)]
    public int SignedUrlLifetimeMinutes { get; init; } = 15;

    [Required]
    public string LocalRootPath { get; init; } = "storage/local";

    public S3StorageOptions S3 { get; init; } = new();
}

public sealed class S3StorageOptions
{
    public string BucketName { get; init; } = string.Empty;
    public string Region { get; init; } = "us-east-1";
    public string? ServiceUrl { get; init; }
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public bool ForcePathStyle { get; init; } = true;
}
