namespace StudentWorkforceManagement.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const long OneGigabyteInBytes = 1_073_741_824;

    public long MaxFileSizeBytes { get; init; } = OneGigabyteInBytes;
    public long StudentStorageQuotaBytes { get; init; }
    public long DepartmentStorageQuotaBytes { get; init; }
}
