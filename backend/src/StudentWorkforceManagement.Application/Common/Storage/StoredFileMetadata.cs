namespace StudentWorkforceManagement.Application.Common.Storage;

public sealed record StoredFileMetadata(string StorageKey, long FileSizeBytes, string MimeType, string? ContentHash);
