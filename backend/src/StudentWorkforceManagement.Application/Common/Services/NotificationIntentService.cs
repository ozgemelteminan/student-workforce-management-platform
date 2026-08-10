using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Interfaces;
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

namespace StudentWorkforceManagement.Application.Common.Services;

public sealed class NotificationIntentService(IApplicationDbContext dbContext) : INotificationIntentService
{
    public async Task CreateAsync(Guid userId, NotificationType type, string title, string message, string? relatedEntityType, Guid? relatedEntityId, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && await dbContext.Notifications.AnyAsync(notification => notification.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            return;
        }

        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IdempotencyKey = idempotencyKey
        });
    }
}
