using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Application.Submissions.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Submissions.Queries.GetSubmission;

public sealed record GetTaskSubmissionsQuery(Guid TaskId) : IRequest<IReadOnlyCollection<SubmissionDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetSubmissionVersionsQuery(Guid SubmissionId) : IRequest<IReadOnlyCollection<SubmissionVersionDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetSubmissionDownloadUrlQuery(Guid SubmissionId) : IRequest<SubmissionDownloadUrlDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetSubmissionVersionDownloadUrlQuery(Guid SubmissionVersionId, Guid? SubmissionId = null) : IRequest<SubmissionDownloadUrlDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetSubmissionQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IFileStorage storage)
    : IRequestHandler<GetTaskSubmissionsQuery, IReadOnlyCollection<SubmissionDto>>,
      IRequestHandler<GetSubmissionVersionsQuery, IReadOnlyCollection<SubmissionVersionDto>>,
      IRequestHandler<GetSubmissionDownloadUrlQuery, SubmissionDownloadUrlDto>,
      IRequestHandler<GetSubmissionVersionDownloadUrlQuery, SubmissionDownloadUrlDto>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<SubmissionDto>> Handle(GetTaskSubmissionsQuery request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
        if (currentUser.IsInRole(UserRole.STUDENT) && task.AssignedStudentId != currentUser.StudentId)
        {
            throw new ForbiddenException("Students may view only their own task submissions.");
        }

        return await dbContext.TaskSubmissions.AsNoTracking()
            .Where(submission => submission.TaskId == request.TaskId)
            .Select(submission => new SubmissionDto(
                submission.Id,
                submission.TaskId,
                submission.SubmittedById,
                submission.Status,
                submission.SubmittedAt,
                submission.ConcurrencyToken,
                submission.Reviews
                    .Where(review => !review.IsApproved)
                    .OrderByDescending(review => review.CreatedAt)
                    .Select(review => review.ReviewerComment)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<SubmissionVersionDto>> Handle(GetSubmissionVersionsQuery request, CancellationToken cancellationToken)
    {
        var submission = await dbContext.TaskSubmissions.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == request.SubmissionId, cancellationToken)
            ?? throw new NotFoundException("TaskSubmission", request.SubmissionId);
        if (currentUser.IsInRole(UserRole.STUDENT) && submission.SubmittedById != currentUser.StudentId)
        {
            throw new ForbiddenException("Students may view only their own submission versions.");
        }

        return await dbContext.SubmissionVersions.AsNoTracking()
            .Where(version => version.TaskSubmissionId == request.SubmissionId)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new SubmissionVersionDto(version.Id, version.TaskSubmissionId, version.VersionNumber, version.File.FileName, version.File.StorageKey, version.File.FileSize, version.File.MimeType, version.File.FileExtension, version.File.ContentHash, version.FileStatus, version.UploadedById, version.UploadedAt, version.ConfirmedAt))
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<SubmissionDownloadUrlDto> Handle(GetSubmissionDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var version = await dbContext.SubmissionVersions.AsNoTracking()
            .Include(item => item.TaskSubmission)
            .Where(item => item.TaskSubmissionId == request.SubmissionId && item.FileStatus == FileStatus.CONFIRMED && item.DeletedAt == null)
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("SubmissionVersion", request.SubmissionId);

        return await CreateDownloadAsync(version, cancellationToken);
    }

    public async System.Threading.Tasks.Task<SubmissionDownloadUrlDto> Handle(GetSubmissionVersionDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var version = await dbContext.SubmissionVersions.AsNoTracking()
            .Include(item => item.TaskSubmission)
            .SingleOrDefaultAsync(item => item.Id == request.SubmissionVersionId, cancellationToken)
            ?? throw new NotFoundException("SubmissionVersion", request.SubmissionVersionId);
        if (request.SubmissionId.HasValue && version.TaskSubmissionId != request.SubmissionId.Value)
        {
            throw new NotFoundException("SubmissionVersion", request.SubmissionVersionId);
        }

        return await CreateDownloadAsync(version, cancellationToken);
    }

    private async System.Threading.Tasks.Task<SubmissionDownloadUrlDto> CreateDownloadAsync(StudentWorkforceManagement.Domain.Entities.SubmissionVersion version, CancellationToken cancellationToken)
    {
        if (version.TaskSubmission is null)
        {
            throw new NotFoundException("TaskSubmission", version.TaskSubmissionId);
        }
        if (currentUser.IsInRole(UserRole.STUDENT) && version.TaskSubmission.SubmittedById != currentUser.StudentId)
        {
            throw new ForbiddenException("Students may download only their own submission files.");
        }
        if (version.FileStatus != FileStatus.CONFIRMED || version.DeletedAt.HasValue)
        {
            throw new ConflictException("Only confirmed submission files can be downloaded.");
        }

        var target = await storage.CreateDownloadTargetAsync(version.File.StorageKey, cancellationToken);
        return new SubmissionDownloadUrlDto(version.Id, version.File.FileName, version.File.FileSize, target.DownloadUrl, target.ExpiresAt);
    }
}
