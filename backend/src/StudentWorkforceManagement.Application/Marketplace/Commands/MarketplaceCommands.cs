using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Events;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Marketplace.DTOs;
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

namespace StudentWorkforceManagement.Application.Marketplace.Commands;

public sealed record PublishTaskToMarketplaceCommand(Guid TaskId, MarketplaceApprovalMode ApprovalMode, DateTimeOffset? ExpiresAt) : IRequest<MarketplaceListingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record UnpublishTaskCommand(Guid ListingId) : IRequest<MarketplaceListingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record ClaimMarketplaceTaskCommand(Guid ListingId, DateTimeOffset? ExpiresAt = null) : IRequest<MarketplaceClaimDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}
public sealed record ApproveMarketplaceClaimCommand(Guid ClaimId) : IRequest<MarketplaceClaimDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record RejectMarketplaceClaimCommand(Guid ClaimId) : IRequest<MarketplaceClaimDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record CancelMarketplaceClaimCommand(Guid ClaimId) : IRequest<MarketplaceClaimDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed class PublishTaskToMarketplaceCommandValidator : AbstractValidator<PublishTaskToMarketplaceCommand>
{
    public PublishTaskToMarketplaceCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.ApprovalMode).IsInEnum();
    }
}

public sealed class MarketplaceCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IAuditService auditService, IApplicationEventQueue events, IUtcClock clock)
    : IRequestHandler<PublishTaskToMarketplaceCommand, MarketplaceListingDto>,
      IRequestHandler<UnpublishTaskCommand, MarketplaceListingDto>,
      IRequestHandler<ClaimMarketplaceTaskCommand, MarketplaceClaimDto>,
      IRequestHandler<ApproveMarketplaceClaimCommand, MarketplaceClaimDto>,
      IRequestHandler<RejectMarketplaceClaimCommand, MarketplaceClaimDto>,
      IRequestHandler<CancelMarketplaceClaimCommand, MarketplaceClaimDto>
{
    public async System.Threading.Tasks.Task<MarketplaceListingDto> Handle(PublishTaskToMarketplaceCommand request, CancellationToken cancellationToken)
    {
        if (await dbContext.MarketplaceListings.AnyAsync(listing => listing.TaskId == request.TaskId && listing.Status == MarketplaceListingStatus.PUBLISHED, cancellationToken))
        {
            throw new ConflictException("Task already has a published marketplace listing.");
        }

        var listing = new MarketplaceListing
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            Status = MarketplaceListingStatus.PUBLISHED,
            ApprovalMode = request.ApprovalMode,
            PublishedAt = clock.UtcNow,
            ExpiresAt = request.ExpiresAt,
            PublishedById = currentUser.RequireUserId()
        };
        dbContext.MarketplaceListings.Add(listing);
        await auditService.RecordAsync("MarketplaceListingPublished", "MarketplaceListing", listing.Id, cancellationToken: cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(listing);
    }

    public async System.Threading.Tasks.Task<MarketplaceListingDto> Handle(UnpublishTaskCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.MarketplaceListings.SingleOrDefaultAsync(entity => entity.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException("MarketplaceListing", request.ListingId);
        listing.Status = MarketplaceListingStatus.UNPUBLISHED;
        listing.UnpublishedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(listing);
    }

    public async System.Threading.Tasks.Task<MarketplaceClaimDto> Handle(ClaimMarketplaceTaskCommand request, CancellationToken cancellationToken)
    {
        var listing = await dbContext.MarketplaceListings.SingleOrDefaultAsync(entity => entity.Id == request.ListingId, cancellationToken)
            ?? throw new NotFoundException("MarketplaceListing", request.ListingId);
        if (listing.Status != MarketplaceListingStatus.PUBLISHED || (listing.ExpiresAt.HasValue && listing.ExpiresAt <= clock.UtcNow))
        {
            throw new ConflictException("Marketplace listing is not claimable.");
        }

        var studentId = currentUser.RequireStudentId();
        var claim = new MarketplaceClaim
        {
            Id = Guid.NewGuid(),
            MarketplaceListingId = listing.Id,
            StudentId = studentId,
            Status = MarketplaceClaimStatus.PENDING,
            ClaimedAt = clock.UtcNow,
            ExpiresAt = request.ExpiresAt
        };
        dbContext.MarketplaceClaims.Add(claim);
        return ToDto(claim);
    }

    public async System.Threading.Tasks.Task<MarketplaceClaimDto> Handle(ApproveMarketplaceClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await dbContext.MarketplaceClaims.SingleOrDefaultAsync(entity => entity.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException("MarketplaceClaim", request.ClaimId);
        var listing = await dbContext.MarketplaceListings.SingleOrDefaultAsync(entity => entity.Id == claim.MarketplaceListingId, cancellationToken)
            ?? throw new NotFoundException("MarketplaceListing", claim.MarketplaceListingId);
        var task = await dbContext.Tasks.SingleOrDefaultAsync(entity => entity.Id == listing.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", listing.TaskId);
        if (claim.Status != MarketplaceClaimStatus.PENDING)
        {
            throw new ConflictException("Only pending marketplace claims can be approved.");
        }

        var actorId = currentUser.RequireUserId();
        claim.Status = MarketplaceClaimStatus.APPROVED;
        claim.ApprovedAt = clock.UtcNow;
        claim.ApprovedById = actorId;
        listing.Status = MarketplaceListingStatus.CLOSED;
        task.AssignedStudentId = claim.StudentId;
        task.Status = TaskStatus.ASSIGNED;
        dbContext.TaskAssignmentHistory.Add(new TaskAssignmentHistory
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            StudentId = claim.StudentId,
            AssignedByUserId = actorId,
            AssignedAt = clock.UtcNow,
            Status = AssignmentStatus.ACTIVE,
            Mode = AssignmentMode.MARKETPLACE,
            IsActive = true,
            Reason = "Marketplace claim approved"
        });
        await auditService.RecordAsync("MarketplaceClaimApproved", "MarketplaceClaim", claim.Id, cancellationToken: cancellationToken);
        events.Enqueue(new MarketplaceClaimAcceptedEvent(listing.Id, claim.Id, claim.StudentId, actorId));
        return ToDto(claim);
    }

    public async System.Threading.Tasks.Task<MarketplaceClaimDto> Handle(RejectMarketplaceClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await dbContext.MarketplaceClaims.SingleOrDefaultAsync(entity => entity.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException("MarketplaceClaim", request.ClaimId);
        claim.Status = MarketplaceClaimStatus.REJECTED;
        claim.RejectedAt = clock.UtcNow;
        claim.RejectedById = currentUser.RequireUserId();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(claim);
    }

    public async System.Threading.Tasks.Task<MarketplaceClaimDto> Handle(CancelMarketplaceClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await dbContext.MarketplaceClaims.SingleOrDefaultAsync(entity => entity.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException("MarketplaceClaim", request.ClaimId);
        if (claim.StudentId != currentUser.StudentId)
        {
            throw new ForbiddenException("Students may cancel only their own marketplace claims.");
        }
        claim.Status = MarketplaceClaimStatus.CANCELLED;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(claim);
    }

    private static MarketplaceListingDto ToDto(MarketplaceListing listing)
    {
        return new MarketplaceListingDto(listing.Id, listing.TaskId, listing.Status, listing.ApprovalMode, listing.PublishedAt, listing.ExpiresAt, listing.ConcurrencyToken, null);
    }

    private static MarketplaceClaimDto ToDto(MarketplaceClaim claim)
    {
        return new MarketplaceClaimDto(claim.Id, claim.MarketplaceListingId, claim.StudentId, claim.Status, claim.ClaimedAt, claim.ExpiresAt, claim.ApprovedAt, claim.RejectedAt, claim.ConcurrencyToken);
    }
}
