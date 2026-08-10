using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Announcements.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Time;
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

namespace StudentWorkforceManagement.Application.Announcements.Commands;

public sealed record CreateAnnouncementCommand(string Title, string Content, DateTimeOffset? ExpiresAt, bool IsPinned) : IRequest<AnnouncementDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => new[] { UserRole.ADMIN, UserRole.TASK_MANAGER };
}
public sealed record PublishAnnouncementCommand(Guid Id) : IRequest<AnnouncementDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => new[] { UserRole.ADMIN, UserRole.TASK_MANAGER };
}
public sealed record UnpublishAnnouncementCommand(Guid Id) : IRequest<AnnouncementDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => new[] { UserRole.ADMIN, UserRole.TASK_MANAGER };
}
public sealed record DeleteAnnouncementCommand(Guid Id) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => new[] { UserRole.ADMIN, UserRole.TASK_MANAGER };
}

public sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Content).NotEmpty().MaximumLength(12000);
    }
}

public sealed class AnnouncementCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IUtcClock clock)
    : IRequestHandler<CreateAnnouncementCommand, AnnouncementDto>,
      IRequestHandler<PublishAnnouncementCommand, AnnouncementDto>,
      IRequestHandler<UnpublishAnnouncementCommand, AnnouncementDto>,
      IRequestHandler<DeleteAnnouncementCommand, Unit>
{
    public async System.Threading.Tasks.Task<AnnouncementDto> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = new Announcement { Id = Guid.NewGuid(), Title = request.Title.Trim(), Content = request.Content.Trim(), ExpiresAt = request.ExpiresAt, IsPinned = request.IsPinned, CreatedById = currentUser.RequireUserId() };
        dbContext.Announcements.Add(announcement);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(announcement);
    }

    public System.Threading.Tasks.Task<AnnouncementDto> Handle(PublishAnnouncementCommand request, CancellationToken cancellationToken) => SetPublishedAsync(request.Id, true, cancellationToken);
    public System.Threading.Tasks.Task<AnnouncementDto> Handle(UnpublishAnnouncementCommand request, CancellationToken cancellationToken) => SetPublishedAsync(request.Id, false, cancellationToken);

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.SingleOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Announcement", request.Id);
        announcement.DeletedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private async System.Threading.Tasks.Task<AnnouncementDto> SetPublishedAsync(Guid id, bool isPublished, CancellationToken cancellationToken)
    {
        var announcement = await dbContext.Announcements.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException("Announcement", id);
        announcement.IsPublished = isPublished;
        announcement.PublishedAt = isPublished ? clock.UtcNow : null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(announcement);
    }

    private static AnnouncementDto ToDto(Announcement announcement) => new(announcement.Id, announcement.Title, announcement.Content, announcement.CreatedById, announcement.ExpiresAt, announcement.IsPinned, announcement.IsPublished, announcement.PublishedAt, announcement.CreatedAt, announcement.UpdatedAt);
}
