using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Files.DTOs;

public sealed record FileUploadIntentDto(Guid FileId, string StorageKey, string FileName, long FileSize, string MimeType, string FileExtension, FileStatus Status, Uri SignedUploadUrl, string UploadMethod, IReadOnlyDictionary<string, string> RequiredHeaders, DateTimeOffset ExpiresAt);

public sealed record DepartmentFileDto(Guid Id, Guid? FolderId, Guid UploadedById, string FileName, string StorageKey, long FileSize, string MimeType, string FileExtension, string? ContentHash, FileStatus Status, DateTimeOffset? ConfirmedAt, DateTimeOffset CreatedAt);

public sealed record FileFolderDto(Guid Id, Guid? ParentFolderId, string Name, DateTimeOffset CreatedAt);

public sealed record AuthorizedDownloadDto(Guid FileId, string StorageKey, Uri DownloadUrl, DateTimeOffset ExpiresAt);
