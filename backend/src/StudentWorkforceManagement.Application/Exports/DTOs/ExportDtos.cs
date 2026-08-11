using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Exports.DTOs;

public sealed record ExportRequestDto(
    Guid Id,
    Guid RequestingUserId,
    ExportType ExportType,
    ExportFormat Format,
    ExportStatus Status,
    Guid? ScopeId,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? FailedAt,
    DateTimeOffset? ExpiresAt,
    string? FailureReason,
    string? ArtifactFileName,
    long? ArtifactFileSize,
    string? ArtifactMimeType,
    Guid ConcurrencyToken);

public sealed record ExportAcceptedDto(Guid Id, ExportStatus Status, Uri StatusUrl);

public sealed record ExportDownloadDto(
    Guid Id,
    string StorageKey,
    string FileName,
    long FileSize,
    string MimeType,
    Uri DownloadUrl,
    DateTimeOffset DownloadUrlExpiresAt);
