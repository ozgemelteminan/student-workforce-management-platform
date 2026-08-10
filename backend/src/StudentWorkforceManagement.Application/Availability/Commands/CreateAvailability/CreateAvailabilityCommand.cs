using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Availability.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
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

namespace StudentWorkforceManagement.Application.Availability.Commands.CreateAvailability;

public sealed record CreateAvailabilityCommand(Guid StudentId, Guid SemesterId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, AvailabilityStatus Status, string? Reason)
    : IRequest<AvailabilityDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class CreateAvailabilityCommandValidator : AbstractValidator<CreateAvailabilityCommand>
{
    public CreateAvailabilityCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.SemesterId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command).Must(command => command.EndTime > command.StartTime).WithMessage("Availability end time must be after start time.");
    }
}
public sealed class CreateAvailabilityCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<CreateAvailabilityCommand, AvailabilityDto>
{
    public async System.Threading.Tasks.Task<AvailabilityDto> Handle(CreateAvailabilityCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new ForbiddenException("Students may edit only their own availability.");
        }
        var overlaps = await dbContext.Availability.AnyAsync(item => item.StudentId == request.StudentId && item.SemesterId == request.SemesterId && item.DayOfWeek == request.DayOfWeek && item.StartTime < request.EndTime && request.StartTime < item.EndTime, cancellationToken);
        if (overlaps)
        {
            throw new ConflictException("Availability overlaps an existing availability record.");
        }
        var availability = new AvailabilityEntity { Id = Guid.NewGuid(), StudentId = request.StudentId, SemesterId = request.SemesterId, DayOfWeek = request.DayOfWeek, StartTime = request.StartTime, EndTime = request.EndTime, Status = request.Status, Reason = request.Reason };
        dbContext.Availability.Add(availability);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AvailabilityDto(availability.Id, availability.StudentId, availability.SemesterId, availability.DayOfWeek, availability.StartTime, availability.EndTime, availability.Status, availability.Reason, availability.ConcurrencyToken);
    }
}
