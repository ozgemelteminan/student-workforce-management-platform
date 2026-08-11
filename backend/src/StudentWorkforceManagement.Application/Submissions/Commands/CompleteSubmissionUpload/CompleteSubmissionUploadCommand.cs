using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Files.Services;
using StudentWorkforceManagement.Application.Submissions.DTOs;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Submissions.Commands.CompleteSubmissionUpload;

public sealed record InitiateSubmissionUploadCommand(
    Guid TaskId,
    string FileName,
    long FileSize,
    string MimeType,
    string FileExtension,
    string? ContentHash) : IRequest<SubmissionUploadIntentDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record CompleteSubmissionUploadCommand(Guid SubmissionVersionId, Guid? TaskId = null) : IRequest<SubmissionVersionDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed class InitiateSubmissionUploadCommandValidator : AbstractValidator<InitiateSubmissionUploadCommand>
{
    public InitiateSubmissionUploadCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(255);
        RuleFor(command => command.FileSize).InclusiveBetween(1, UploadFilePolicyOptions.OneGigabyteInBytes);
        RuleFor(command => command.MimeType).NotEmpty().MaximumLength(150);
        RuleFor(command => command.FileExtension).NotEmpty().MaximumLength(20);
        RuleFor(command => command.ContentHash).MaximumLength(128);
    }
}

public sealed class CompleteSubmissionUploadCommandValidator : AbstractValidator<CompleteSubmissionUploadCommand>
{
    public CompleteSubmissionUploadCommandValidator()
    {
        RuleFor(command => command.SubmissionVersionId).NotEmpty();
    }
}

public sealed class CompleteSubmissionUploadCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IFileStorage storage, IUploadFilePolicy uploadFilePolicy, IUtcClock clock)
    : IRequestHandler<InitiateSubmissionUploadCommand, SubmissionUploadIntentDto>, IRequestHandler<CompleteSubmissionUploadCommand, SubmissionVersionDto>
{
    public async System.Threading.Tasks.Task<SubmissionUploadIntentDto> Handle(InitiateSubmissionUploadCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.RequireStudentId();
        var task = await dbContext.Tasks.SingleOrDefaultAsync(entity => entity.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
        if (task.AssignedStudentId != studentId)
        {
            throw new ForbiddenException("Students may upload submissions only for their assigned tasks.");
        }
        if (task.Status is TaskStatus.CANCELLED or TaskStatus.COMPLETED)
        {
            throw new ConflictException("Submissions are not allowed for cancelled or completed tasks.");
        }

        var upload = uploadFilePolicy.ValidatePendingUpload(request.FileName, request.FileSize, request.MimeType, request.FileExtension);
        var submission = await dbContext.TaskSubmissions.SingleOrDefaultAsync(entity => entity.TaskId == request.TaskId && entity.SubmittedById == studentId, cancellationToken);
        if (submission is null)
        {
            submission = new TaskSubmission
            {
                Id = Guid.NewGuid(),
                TaskId = request.TaskId,
                SubmittedById = studentId,
                Status = SubmissionStatus.DRAFT
            };
            dbContext.TaskSubmissions.Add(submission);
        }

        var nextVersion = await dbContext.SubmissionVersions
            .Where(version => version.TaskSubmissionId == submission.Id)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var target = await storage.CreateUploadTargetAsync(new UploadTargetRequest(upload.FileName, upload.FileSizeBytes, upload.MimeType, upload.FileExtension, "task-submissions", false), cancellationToken);
        var version = new SubmissionVersion
        {
            Id = Guid.NewGuid(),
            TaskSubmissionId = submission.Id,
            VersionNumber = nextVersion + 1,
            File = new FileMetadata
            {
                FileName = upload.FileName,
                StorageKey = target.StorageKey,
                FileSize = upload.FileSizeBytes,
                MimeType = upload.MimeType,
                FileExtension = upload.FileExtension,
                ContentHash = request.ContentHash
            },
            FileStatus = FileStatus.UPLOAD_PENDING,
            UploadedById = studentId,
            UploadedAt = clock.UtcNow
        };
        dbContext.SubmissionVersions.Add(version);
        return new SubmissionUploadIntentDto(version.Id, submission.Id, version.VersionNumber, target.StorageKey, version.File.FileName, version.File.FileSize, version.File.MimeType, version.File.FileExtension, version.FileStatus, target.UploadUrl, target.UploadMethod, target.RequiredHeaders ?? new Dictionary<string, string>(), target.ExpiresAt);
    }

    public async System.Threading.Tasks.Task<SubmissionVersionDto> Handle(CompleteSubmissionUploadCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.RequireStudentId();
        var version = await dbContext.SubmissionVersions.Include(item => item.TaskSubmission).SingleOrDefaultAsync(item => item.Id == request.SubmissionVersionId, cancellationToken)
            ?? throw new NotFoundException("SubmissionVersion", request.SubmissionVersionId);
        if (version.TaskSubmission?.SubmittedById != studentId)
        {
            throw new ForbiddenException("Students may complete only their own uploads.");
        }
        if (request.TaskId.HasValue && version.TaskSubmission.TaskId != request.TaskId.Value)
        {
            throw new NotFoundException("SubmissionVersion", request.SubmissionVersionId);
        }
        if (version.FileStatus == FileStatus.CONFIRMED)
        {
            return ToDto(version);
        }
        if (version.FileStatus != FileStatus.UPLOAD_PENDING && version.FileStatus != FileStatus.UPLOADED)
        {
            throw new ConflictException("Submission version is not in a completable state.");
        }
        var metadata = await storage.GetMetadataAsync(version.File.StorageKey, cancellationToken)
            ?? throw new ConflictException("Uploaded object metadata could not be verified.");
        uploadFilePolicy.ValidateStoredObject(version.File, metadata);
        if (!string.IsNullOrWhiteSpace(version.File.ContentHash) && !string.Equals(version.File.ContentHash, metadata.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Uploaded object content hash does not match the pending submission metadata.");
        }
        version.FileStatus = FileStatus.CONFIRMED;
        version.ConfirmedAt = clock.UtcNow;
        if (version.TaskSubmission is not null && version.TaskSubmission.Status == SubmissionStatus.DRAFT)
        {
            version.TaskSubmission.Status = SubmissionStatus.SUBMITTED_FOR_REVIEW;
            version.TaskSubmission.SubmittedAt = clock.UtcNow;
        }
        return ToDto(version);
    }

    private static SubmissionVersionDto ToDto(SubmissionVersion version)
    {
        return new SubmissionVersionDto(version.Id, version.TaskSubmissionId, version.VersionNumber, version.File.FileName, version.File.StorageKey, version.File.FileSize, version.File.MimeType, version.File.FileExtension, version.File.ContentHash, version.FileStatus, version.UploadedById, version.UploadedAt, version.ConfirmedAt);
    }
}
