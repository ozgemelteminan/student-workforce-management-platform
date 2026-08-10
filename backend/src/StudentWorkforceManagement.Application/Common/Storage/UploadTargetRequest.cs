namespace StudentWorkforceManagement.Application.Common.Storage;

public sealed record UploadTargetRequest(
    string FileName,
    long FileSizeBytes,
    string MimeType,
    string FileExtension,
    string OwnershipScope,
    bool RequiresMultipartUpload);
