using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
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

public sealed class GetSubmissionQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetTaskSubmissionsQuery, IReadOnlyCollection<SubmissionDto>>,
      IRequestHandler<GetSubmissionVersionsQuery, IReadOnlyCollection<SubmissionVersionDto>>
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
            .Select(submission => new SubmissionDto(submission.Id, submission.TaskId, submission.SubmittedById, submission.Status, submission.SubmittedAt, submission.ConcurrencyToken))
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
}
