using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Infrastructure.Notifications.SignalR;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    INotificationRealtimeDispatcher? notificationRealtimeDispatcher = null,
    ILogger<ApplicationDbContext>? logger = null) : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<StudentSkill> StudentSkills => Set<StudentSkill>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<CourseSchedule> CourseSchedules => Set<CourseSchedule>();
    public DbSet<Availability> Availability => Set<Availability>();
    public DbSet<DomainTask> Tasks => Set<DomainTask>();
    public DbSet<TaskAssignmentHistory> TaskAssignmentHistory => Set<TaskAssignmentHistory>();
    public DbSet<TaskRequiredSkill> TaskRequiredSkills => Set<TaskRequiredSkill>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskChecklistItem> TaskChecklistItems => Set<TaskChecklistItem>();
    public DbSet<TaskSubmission> TaskSubmissions => Set<TaskSubmission>();
    public DbSet<SubmissionVersion> SubmissionVersions => Set<SubmissionVersion>();
    public DbSet<TaskRequest> TaskRequests => Set<TaskRequest>();
    public DbSet<TaskReview> TaskReviews => Set<TaskReview>();
    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();
    public DbSet<MarketplaceClaim> MarketplaceClaims => Set<MarketplaceClaim>();
    public DbSet<FileFolder> FileFolders => Set<FileFolder>();
    public DbSet<DepartmentFile> DepartmentFiles => Set<DepartmentFile>();
    public DbSet<ExportRequest> ExportRequests => Set<ExportRequest>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();
    public DbSet<RecurringTask> RecurringTasks => Set<RecurringTask>();
    public DbSet<RecurringTaskOccurrence> RecurringTaskOccurrences => Set<RecurringTaskOccurrence>();
    public DbSet<EmailDelivery> EmailDeliveries => Set<EmailDelivery>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<TimesheetWeek> TimesheetWeeks => Set<TimesheetWeek>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<TemporaryUnavailability> TemporaryUnavailability => Set<TemporaryUnavailability>();
    public DbSet<TaskNudge> TaskNudges => Set<TaskNudge>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();
    public DbSet<MeetingActionItem> MeetingActionItems => Set<MeetingActionItem>();



    public async Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new EfApplicationTransaction(transaction);
    }

    private sealed class EfApplicationTransaction(IDbContextTransaction transaction) : IApplicationTransaction
    {
        public System.Threading.Tasks.Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

        public System.Threading.Tasks.Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var createdNotifications = ChangeTracker.Entries<Notification>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (notificationRealtimeDispatcher is not null)
        {
            foreach (var notification in createdNotifications)
            {
                try
                {
                    await notificationRealtimeDispatcher.DispatchAsync(notification, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger?.LogWarning(exception, "Realtime notification dispatch failed for notification {NotificationId}", notification.Id);
                }
            }
        }

        return result;
    }
}
