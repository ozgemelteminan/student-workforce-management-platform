namespace StudentWorkforceManagement.Application.Common.Storage;

public sealed record SignedDownloadTarget(Uri DownloadUrl, DateTimeOffset ExpiresAt);
