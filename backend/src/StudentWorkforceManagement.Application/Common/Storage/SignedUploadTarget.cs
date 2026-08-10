namespace StudentWorkforceManagement.Application.Common.Storage;

public sealed record SignedUploadTarget(
    Guid UploadId,
    string StorageKey,
    Uri UploadUrl,
    DateTimeOffset ExpiresAt,
    bool IsMultipart);
