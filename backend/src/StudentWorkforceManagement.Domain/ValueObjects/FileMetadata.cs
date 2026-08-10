namespace StudentWorkforceManagement.Domain.ValueObjects;

public sealed record FileMetadata
{
    public string FileName { get; init; } = string.Empty;
    public string StorageKey { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public string FileExtension { get; init; } = string.Empty;
    public string? ContentHash { get; init; }
}
