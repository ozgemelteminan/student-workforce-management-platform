using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Submissions.DTOs;

public sealed record SubmissionDto(Guid Id, Guid TaskId, Guid SubmittedById, SubmissionStatus Status, DateTimeOffset? SubmittedAt, Guid ConcurrencyToken);
public sealed record SubmissionUploadIntentDto(Guid SubmissionVersionId, Guid TaskSubmissionId, int VersionNumber, string StorageKey, string FileName, long FileSize, string MimeType, string FileExtension, FileStatus FileStatus);
public sealed record SubmissionVersionDto(Guid Id, Guid TaskSubmissionId, int VersionNumber, string FileName, string StorageKey, long FileSize, string MimeType, string FileExtension, string? ContentHash, FileStatus FileStatus, Guid UploadedById, DateTimeOffset UploadedAt, DateTimeOffset? ConfirmedAt);
public sealed record TaskReviewDto(Guid Id, Guid TaskId, Guid SubmissionId, Guid ReviewedById, bool IsApproved, string? ReviewerComment, DateTimeOffset CreatedAt);
