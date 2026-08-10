using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Submissions.DTOs;
using Announcement = StudentWorkforceManagement.Domain.Entities.Announcement;
using AuditLog = StudentWorkforceManagement.Domain.Entities.AuditLog;
using AvailabilityEntity = StudentWorkforceManagement.Domain.Entities.Availability;
using Category = StudentWorkforceManagement.Domain.Entities.Category;
using CourseSchedule = StudentWorkforceManagement.Domain.Entities.CourseSchedule;
using EmailDelivery = StudentWorkforceManagement.Domain.Entities.EmailDelivery;
using MarketplaceClaim = StudentWorkforceManagement.Domain.Entities.MarketplaceClaim;
using MarketplaceListing = StudentWorkforceManagement.Domain.Entities.MarketplaceListing;
using Notification = StudentWorkforceManagement.Domain.Entities.Notification;
using NotificationPreference = StudentWorkforceManagement.Domain.Entities.NotificationPreference;
using Semester = StudentWorkforceManagement.Domain.Entities.Semester;
using Skill = StudentWorkforceManagement.Domain.Entities.Skill;
using StudentSkill = StudentWorkforceManagement.Domain.Entities.StudentSkill;
using SubmissionVersion = StudentWorkforceManagement.Domain.Entities.SubmissionVersion;
using SystemSetting = StudentWorkforceManagement.Domain.Entities.SystemSetting;
using TaskAssignmentHistory = StudentWorkforceManagement.Domain.Entities.TaskAssignmentHistory;
using TaskChecklistItem = StudentWorkforceManagement.Domain.Entities.TaskChecklistItem;
using TaskComment = StudentWorkforceManagement.Domain.Entities.TaskComment;
using TaskDependency = StudentWorkforceManagement.Domain.Entities.TaskDependency;
using TaskRequest = StudentWorkforceManagement.Domain.Entities.TaskRequest;
using TaskReview = StudentWorkforceManagement.Domain.Entities.TaskReview;
using TaskSubmission = StudentWorkforceManagement.Domain.Entities.TaskSubmission;
using DepartmentFile = StudentWorkforceManagement.Domain.Entities.DepartmentFile;
using Feedback = StudentWorkforceManagement.Domain.Entities.Feedback;
using FileFolder = StudentWorkforceManagement.Domain.Entities.FileFolder;
using Invitation = StudentWorkforceManagement.Domain.Entities.Invitation;
using RecurringTask = StudentWorkforceManagement.Domain.Entities.RecurringTask;
using RefreshToken = StudentWorkforceManagement.Domain.Entities.RefreshToken;
using Role = StudentWorkforceManagement.Domain.Entities.Role;
using Session = StudentWorkforceManagement.Domain.Entities.Session;
using Student = StudentWorkforceManagement.Domain.Entities.Student;
using TaskRequiredSkill = StudentWorkforceManagement.Domain.Entities.TaskRequiredSkill;
using TaskTemplate = StudentWorkforceManagement.Domain.Entities.TaskTemplate;
using User = StudentWorkforceManagement.Domain.Entities.User;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Application.Submissions.Commands.CompleteSubmissionUpload;

public sealed record CompleteSubmissionUploadCommand(
    Guid TaskId,
    string FileName,
    string StorageKey,
    long FileSize,
    string MimeType,
    string FileExtension,
    string? ContentHash) : IRequest<SubmissionVersionDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed class CompleteSubmissionUploadCommandValidator : AbstractValidator<CompleteSubmissionUploadCommand>
{
    public const long MaxFileSizeBytes = 1024L * 1024L * 1024L;

    public CompleteSubmissionUploadCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(255);
        RuleFor(command => command.StorageKey).NotEmpty().MaximumLength(1024);
        RuleFor(command => command.FileSize).InclusiveBetween(1, MaxFileSizeBytes);
        RuleFor(command => command.MimeType).NotEmpty().MaximumLength(150);
        RuleFor(command => command.FileExtension).NotEmpty().MaximumLength(20);
        RuleFor(command => command.ContentHash).MaximumLength(128);
    }
}

public sealed class CompleteSubmissionUploadCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IUtcClock clock)
    : IRequestHandler<CompleteSubmissionUploadCommand, SubmissionVersionDto>
{
    public async System.Threading.Tasks.Task<SubmissionVersionDto> Handle(CompleteSubmissionUploadCommand request, CancellationToken cancellationToken)
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

        var now = clock.UtcNow;
        var version = new SubmissionVersion
        {
            Id = Guid.NewGuid(),
            TaskSubmissionId = submission.Id,
            VersionNumber = nextVersion + 1,
            File = new FileMetadata
            {
                FileName = request.FileName.Trim(),
                StorageKey = request.StorageKey,
                FileSize = request.FileSize,
                MimeType = request.MimeType,
                FileExtension = request.FileExtension,
                ContentHash = request.ContentHash
            },
            FileStatus = FileStatus.CONFIRMED,
            UploadedById = studentId,
            UploadedAt = now,
            ConfirmedAt = now
        };
        dbContext.SubmissionVersions.Add(version);
        return ToDto(version);
    }

    private static SubmissionVersionDto ToDto(SubmissionVersion version)
    {
        return new SubmissionVersionDto(version.Id, version.TaskSubmissionId, version.VersionNumber, version.File.FileName, version.File.StorageKey, version.File.FileSize, version.File.MimeType, version.File.FileExtension, version.File.ContentHash, version.FileStatus, version.UploadedById, version.UploadedAt, version.ConfirmedAt);
    }
}
