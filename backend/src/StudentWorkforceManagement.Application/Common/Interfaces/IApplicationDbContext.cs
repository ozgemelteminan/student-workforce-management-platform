using Microsoft.EntityFrameworkCore;
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
using ExportRequest = StudentWorkforceManagement.Domain.Entities.ExportRequest;
using Feedback = StudentWorkforceManagement.Domain.Entities.Feedback;
using FileFolder = StudentWorkforceManagement.Domain.Entities.FileFolder;
using Invitation = StudentWorkforceManagement.Domain.Entities.Invitation;
using RecurringTaskOccurrence = StudentWorkforceManagement.Domain.Entities.RecurringTaskOccurrence;
using RecurringTask = StudentWorkforceManagement.Domain.Entities.RecurringTask;
using RefreshToken = StudentWorkforceManagement.Domain.Entities.RefreshToken;
using PasswordResetToken = StudentWorkforceManagement.Domain.Entities.PasswordResetToken;
using Role = StudentWorkforceManagement.Domain.Entities.Role;
using Session = StudentWorkforceManagement.Domain.Entities.Session;
using Student = StudentWorkforceManagement.Domain.Entities.Student;
using TaskRequiredSkill = StudentWorkforceManagement.Domain.Entities.TaskRequiredSkill;
using TaskTemplate = StudentWorkforceManagement.Domain.Entities.TaskTemplate;
using User = StudentWorkforceManagement.Domain.Entities.User;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Student> Students { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<Session> Sessions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<Skill> Skills { get; }
    DbSet<StudentSkill> StudentSkills { get; }
    DbSet<Category> Categories { get; }
    DbSet<Semester> Semesters { get; }
    DbSet<CourseSchedule> CourseSchedules { get; }
    DbSet<AvailabilityEntity> Availability { get; }
    DbSet<DomainTask> Tasks { get; }
    DbSet<TaskAssignmentHistory> TaskAssignmentHistory { get; }
    DbSet<TaskRequiredSkill> TaskRequiredSkills { get; }
    DbSet<TaskDependency> TaskDependencies { get; }
    DbSet<TaskComment> TaskComments { get; }
    DbSet<TaskChecklistItem> TaskChecklistItems { get; }
    DbSet<TaskSubmission> TaskSubmissions { get; }
    DbSet<SubmissionVersion> SubmissionVersions { get; }
    DbSet<TaskRequest> TaskRequests { get; }
    DbSet<TaskReview> TaskReviews { get; }
    DbSet<MarketplaceListing> MarketplaceListings { get; }
    DbSet<MarketplaceClaim> MarketplaceClaims { get; }
    DbSet<FileFolder> FileFolders { get; }
    DbSet<DepartmentFile> DepartmentFiles { get; }
    DbSet<ExportRequest> ExportRequests { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<StudentWorkforceManagement.Domain.Entities.Feedback> Feedback { get; }
    DbSet<TaskTemplate> TaskTemplates { get; }
    DbSet<RecurringTask> RecurringTasks { get; }
    DbSet<RecurringTaskOccurrence> RecurringTaskOccurrences { get; }
    DbSet<EmailDelivery> EmailDeliveries { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SystemSetting> SystemSettings { get; }

    System.Threading.Tasks.Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
