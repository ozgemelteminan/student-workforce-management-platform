using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Analytics.Queries;
using StudentWorkforceManagement.Application.Auth.Commands;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Application.Common.Email;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Files.Commands;
using StudentWorkforceManagement.Application.Files.Services;
using StudentWorkforceManagement.Application.Notifications.Commands;
using StudentWorkforceManagement.Application.Notifications.Queries.GetNotificationPreferences;
using StudentWorkforceManagement.Application.Students.Queries;
using StudentWorkforceManagement.Application.Submissions.Commands.CompleteSubmissionUpload;
using StudentWorkforceManagement.Application.Submissions.Queries.GetSubmission;
using StudentWorkforceManagement.Application.Tasks.Services;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Persistence;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.IntegrationTests.Application;

public sealed class ApplicationCompletionTests
{
    [Fact]
    public async System.Threading.Tasks.Task Invitation_acceptance_hashes_token_and_blocks_replay()
    {
        await using var context = CreateContext();
        var admin = new FakeCurrentUser(Guid.NewGuid(), null, UserRole.ADMIN);
        var tokenGenerator = new FakeTokenGenerator("raw-invite-token");
        var email = new CapturingEmailService();
        var handler = new InvitationCommandHandler(context, admin, tokenGenerator, new FakeClock(), email, new AuditService(context, admin), new FakePasswordService());
        context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = UserRole.STUDENT });
        await context.SaveChangesAsync();

        var created = await handler.Handle(new InviteStudentCommand("Ada@example.com", DateTimeOffset.UtcNow.AddDays(2)), CancellationToken.None);
        var invitation = await context.Invitations.SingleAsync();

        Assert.Equal("raw-invite-token", created.RawToken);
        Assert.Equal(tokenGenerator.HashToken("raw-invite-token"), invitation.TokenHash);
        Assert.DoesNotContain("raw-invite-token", invitation.TokenHash);
        Assert.Equal("raw-invite-token", email.LastMessage?.SecretTemplateData?["invitationToken"]);
        Assert.False(email.LastMessage?.TemplateData.ContainsKey("invitationToken"));
        Assert.Equal("\"ada@example.com\"", (await context.AuditLogs.SingleAsync()).NewValue);

        await handler.Handle(new AcceptInvitationCommand("raw-invite-token", "Password1", "Ada Lovelace", "Ada", "Lovelace", "Computer Engineering"), CancellationToken.None);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new AcceptInvitationCommand("raw-invite-token", "Password1", "Ada Lovelace", "Ada", "Lovelace", "Computer Engineering"), CancellationToken.None));
    }


    [Fact]
    public async System.Threading.Tasks.Task Login_rejects_invalid_or_inactive_and_creates_session_refresh_token_for_valid_credentials()
    {
        await using var context = CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = UserRole.STUDENT };
        var user = new User { Id = Guid.NewGuid(), Email = "login@example.com", DisplayName = "Login User", RoleId = role.Id, PasswordHash = "hash:Password1", IsActive = true, Role = role };
        context.Roles.Add(role);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var handler = new PasswordAuthContractHandler(context, new FakeTokenGenerator("refresh-token"), new FakePasswordService(), new FakeAccessTokenService(), new FakeClock(), new CapturingEmailService(), new AuditService(context, new FakeCurrentUser(user.Id, null, UserRole.STUDENT)));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new LoginCommand("login@example.com", "wrong", "Laptop", "127.0.0.1", DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7)), CancellationToken.None));

        var result = await handler.Handle(new LoginCommand("LOGIN@example.com", "Password1", "Laptop", "127.0.0.1", DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7)), CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RawRefreshToken);
        Assert.Single(context.Sessions);
        Assert.Single(context.RefreshTokens);
        Assert.DoesNotContain("refresh-token", context.RefreshTokens.Single().TokenHash, StringComparison.Ordinal);

        user.IsActive = false;
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new LoginCommand("login@example.com", "Password1", null, null, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7)), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Password_reset_request_and_reset_are_hash_only_and_replay_safe()
    {
        await using var context = CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = UserRole.STUDENT };
        var user = new User { Id = Guid.NewGuid(), Email = "reset-flow@example.com", DisplayName = "Reset Flow", RoleId = role.Id, PasswordHash = "hash:OldPass1", IsActive = true };
        var session = new Session { Id = Guid.NewGuid(), UserId = user.Id, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        context.Roles.Add(role);
        context.Users.Add(user);
        context.Sessions.Add(session);
        context.RefreshTokens.Add(new RefreshToken { Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = "old-refresh-hash", ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });
        await context.SaveChangesAsync();
        var email = new CapturingEmailService();
        var tokenGenerator = new FakeTokenGenerator("reset-token");
        var handler = new PasswordAuthContractHandler(context, tokenGenerator, new FakePasswordService(), new FakeAccessTokenService(), new FakeClock(), email, new AuditService(context, new FakeCurrentUser(user.Id, null, UserRole.STUDENT)));

        await handler.Handle(new ForgotPasswordCommand("RESET-FLOW@example.com", DateTimeOffset.UtcNow.AddMinutes(30)), CancellationToken.None);
        var resetToken = await context.PasswordResetTokens.SingleAsync();

        Assert.Equal(tokenGenerator.HashToken("reset-token"), resetToken.TokenHash);
        Assert.DoesNotContain("reset-token", resetToken.TokenHash, StringComparison.Ordinal);
        Assert.Equal("reset-token", email.LastMessage?.SecretTemplateData?["resetToken"]);
        Assert.False(email.LastMessage?.TemplateData.ContainsKey("resetToken"));

        var result = await handler.Handle(new ResetPasswordCommand("reset-token", "NewPass1"), CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("hash:NewPass1", user.PasswordHash);
        Assert.NotNull(resetToken.ConsumedAt);
        Assert.NotNull(session.RevokedAt);
        Assert.NotNull(context.RefreshTokens.Single().RevokedAt);
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new ResetPasswordCommand("reset-token", "OtherPass1"), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Refresh_rotation_and_session_revocation_are_replay_safe_and_authorized()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var session = new Session { Id = Guid.NewGuid(), UserId = userId, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        context.Sessions.Add(session);
        context.Sessions.Add(new Session { Id = Guid.NewGuid(), UserId = otherUserId, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });
        var tokenGenerator = new FakeTokenGenerator("new-refresh-token");
        context.RefreshTokens.Add(new RefreshToken { Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = tokenGenerator.HashToken("old-refresh-token"), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });
        await context.SaveChangesAsync();
        var role = new Role { Id = Guid.NewGuid(), Name = UserRole.STUDENT };
        var user = new User { Id = userId, Email = "refresh@example.com", DisplayName = "Refresh User", RoleId = role.Id, Role = role, IsActive = true };
        context.Roles.Add(role);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var handler = new SessionCommandHandler(context, new FakeCurrentUser(userId, null, UserRole.STUDENT), tokenGenerator, new FakeAccessTokenService(), new FakeClock());

        var rotated = await handler.Handle(new RefreshTokenCommand("old-refresh-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(session.Id, rotated.SessionId);
        Assert.Equal("access-token", rotated.AccessToken);
        Assert.Equal("new-refresh-token", rotated.RawRefreshToken);
        Assert.Equal(2, context.RefreshTokens.Count());
        Assert.NotNull(context.RefreshTokens.Single(token => token.TokenHash == tokenGenerator.HashToken("old-refresh-token")).ReplacedAt);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new RefreshTokenCommand("old-refresh-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new LogoutCommand(context.Sessions.Single(item => item.UserId == otherUserId).Id), CancellationToken.None));
        await handler.Handle(new LogoutCommand(session.Id), CancellationToken.None);
        Assert.NotNull(session.RevokedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task Refresh_rejects_invalid_expired_revoked_session_and_inactive_user_safely()
    {
        await using var context = CreateContext();
        var role = new Role { Id = Guid.NewGuid(), Name = UserRole.STUDENT };
        var activeUser = new User { Id = Guid.NewGuid(), Email = "active-refresh@example.com", DisplayName = "Active Refresh", RoleId = role.Id, Role = role, IsActive = true };
        var inactiveUser = new User { Id = Guid.NewGuid(), Email = "inactive-refresh@example.com", DisplayName = "Inactive Refresh", RoleId = role.Id, Role = role, IsActive = false };
        var activeSession = new Session { Id = Guid.NewGuid(), UserId = activeUser.Id, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        var revokedSession = new Session { Id = Guid.NewGuid(), UserId = activeUser.Id, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), RevokedAt = DateTimeOffset.UtcNow };
        var expiredSession = new Session { Id = Guid.NewGuid(), UserId = activeUser.Id, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5) };
        var inactiveUserSession = new Session { Id = Guid.NewGuid(), UserId = inactiveUser.Id, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        var tokenGenerator = new FakeTokenGenerator("new-refresh-token");
        context.Roles.Add(role);
        context.Users.AddRange(activeUser, inactiveUser);
        context.Sessions.AddRange(activeSession, revokedSession, expiredSession, inactiveUserSession);
        context.RefreshTokens.AddRange(
            new RefreshToken { Id = Guid.NewGuid(), SessionId = activeSession.Id, TokenHash = tokenGenerator.HashToken("expired-token"), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5) },
            new RefreshToken { Id = Guid.NewGuid(), SessionId = revokedSession.Id, TokenHash = tokenGenerator.HashToken("revoked-session-token"), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) },
            new RefreshToken { Id = Guid.NewGuid(), SessionId = expiredSession.Id, TokenHash = tokenGenerator.HashToken("expired-session-token"), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) },
            new RefreshToken { Id = Guid.NewGuid(), SessionId = inactiveUserSession.Id, TokenHash = tokenGenerator.HashToken("inactive-user-token"), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });
        await context.SaveChangesAsync();
        var handler = new SessionCommandHandler(context, new FakeCurrentUser(activeUser.Id, null, UserRole.STUDENT), tokenGenerator, new FakeAccessTokenService(), new FakeClock());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new RefreshTokenCommand("missing-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new RefreshTokenCommand("expired-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new RefreshTokenCommand("revoked-session-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new RefreshTokenCommand("expired-session-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new RefreshTokenCommand("inactive-user-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Student_cannot_read_another_student_profile()
    {
        await using var context = CreateContext();
        var owner = SeedStudent(context, "owner@example.com");
        var other = SeedStudent(context, "other@example.com");
        await context.SaveChangesAsync();
        var handler = new StudentQueryHandler(context, new FakeCurrentUser(Guid.NewGuid(), owner.Id, UserRole.STUDENT), new TaskWorkloadService(context));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new GetStudentQuery(other.Id), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Department_file_upload_uses_server_storage_key_and_completion_is_idempotent()
    {
        await using var context = CreateContext();
        var storage = new FakeFileStorage();
        var handler = new FileCommandHandler(context, new FakeCurrentUser(Guid.NewGuid(), null, UserRole.ADMIN), storage, CreateUploadPolicy(), new FakeClock());

        var initiated = await handler.Handle(new InitiateDepartmentFileUploadCommand(null, "report.pdf", 4096, "application/pdf", ".pdf", "abc123"), CancellationToken.None);
        storage.Metadata[initiated.StorageKey] = new StoredFileMetadata(initiated.StorageKey, 4096, "application/pdf", "abc123");

        Assert.StartsWith("department-files/", initiated.StorageKey);
        Assert.DoesNotContain("report", initiated.StorageKey, StringComparison.OrdinalIgnoreCase);

        var completed = await handler.Handle(new CompleteDepartmentFileUploadCommand(initiated.FileId), CancellationToken.None);
        var completedAgain = await handler.Handle(new CompleteDepartmentFileUploadCommand(initiated.FileId), CancellationToken.None);

        Assert.Equal(FileStatus.CONFIRMED, completed.Status);
        Assert.Equal(completed.Id, completedAgain.Id);
        Assert.Single(context.DepartmentFiles);
    }

    [Fact]
    public void Upload_policy_allows_required_user_file_types_and_blocks_executables()
    {
        var policy = CreateUploadPolicy();

        Assert.Equal(".docx", policy.ValidatePendingUpload("brief.docx", 1024, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx").FileExtension);
        Assert.Equal(".odt", policy.ValidatePendingUpload("brief.odt", 1024, "application/vnd.oasis.opendocument.text", ".odt").FileExtension);
        Assert.Equal(".json", policy.ValidatePendingUpload("payload.json", 1024, "application/json", ".json").FileExtension);
        Assert.Equal(".webp", policy.ValidatePendingUpload("image.webp", 1024, "image/webp", ".webp").FileExtension);

        Assert.Throws<ConflictException>(() => policy.ValidatePendingUpload("installer.exe", 1024, "application/octet-stream", ".exe"));
        Assert.Throws<ConflictException>(() => policy.ValidatePendingUpload("report.pdf", 1024, "application/msword", ".pdf"));
    }

    [Fact]
    public async System.Threading.Tasks.Task Notification_preferences_read_returns_persisted_rows_without_side_effects_and_reflects_put()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser(userId, null, UserRole.STUDENT);
        var queryHandler = new GetNotificationPreferencesQueryHandler(context, currentUser);
        var commandHandler = new NotificationCommandHandler(context, currentUser, new FakeClock());

        var fresh = await queryHandler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        Assert.Empty(fresh);
        Assert.Empty(context.NotificationPreferences);

        await commandHandler.Handle(new UpdateNotificationPreferenceCommand(NotificationPreferenceType.TaskAssigned, NotificationChannel.EMAIL, false), CancellationToken.None);
        await commandHandler.Handle(new UpdateNotificationPreferenceCommand(NotificationPreferenceType.Announcement, NotificationChannel.IN_APP, true), CancellationToken.None);

        var updated = await queryHandler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);
        var emailTaskAssigned = updated.Single(preference => preference.PreferenceType == NotificationPreferenceType.TaskAssigned && preference.Channel == NotificationChannel.EMAIL);
        var inAppAnnouncement = updated.Single(preference => preference.PreferenceType == NotificationPreferenceType.Announcement && preference.Channel == NotificationChannel.IN_APP);

        Assert.Equal(2, updated.Count);
        Assert.False(emailTaskAssigned.IsEnabled);
        Assert.True(inAppAnnouncement.IsEnabled);
        Assert.Equal(2, context.NotificationPreferences.Count());
    }


    [Fact]
    public async System.Threading.Tasks.Task Submission_upload_uses_server_storage_key_and_idempotent_completion()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context, "submitter@example.com");
        var category = new Category { Id = Guid.NewGuid(), Name = "Submissions" };
        context.Categories.Add(category);
        var task = new StudentWorkforceManagement.Domain.Entities.Task { Id = Guid.NewGuid(), Title = "Upload", CategoryId = category.Id, CreatedById = Guid.NewGuid(), AssignedStudentId = student.Id, Priority = TaskPriority.MEDIUM, Difficulty = TaskDifficulty.EASY, Status = TaskStatus.IN_PROGRESS, Deadline = DateTimeOffset.UtcNow.AddDays(2), EstimatedDurationMinutes = 60 };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var storage = new FakeFileStorage();
        var handler = new CompleteSubmissionUploadCommandHandler(context, new FakeCurrentUser(Guid.NewGuid(), student.Id, UserRole.STUDENT), storage, CreateUploadPolicy(), new FakeClock());

        var initiated = await handler.Handle(new InitiateSubmissionUploadCommand(task.Id, "deliverable.pdf", 2048, "application/pdf", ".pdf", "hash-1"), CancellationToken.None);
        await context.SaveChangesAsync();
        storage.Metadata[initiated.StorageKey] = new StoredFileMetadata(initiated.StorageKey, 2048, "application/pdf", "hash-1");

        Assert.StartsWith("task-submissions/", initiated.StorageKey);
        Assert.DoesNotContain("deliverable", initiated.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(initiated.SignedUploadUrl);
        Assert.Equal("PUT", initiated.UploadMethod);
        Assert.True(initiated.ExpiresAt > DateTimeOffset.UtcNow);

        var completed = await handler.Handle(new CompleteSubmissionUploadCommand(initiated.SubmissionVersionId), CancellationToken.None);
        var completedAgain = await handler.Handle(new CompleteSubmissionUploadCommand(initiated.SubmissionVersionId), CancellationToken.None);

        Assert.Equal(FileStatus.CONFIRMED, completed.FileStatus);
        Assert.Equal(completed.Id, completedAgain.Id);
        Assert.Single(context.SubmissionVersions);
    }

    [Fact]
    public async System.Threading.Tasks.Task Submission_download_url_requires_ownership_and_confirmed_file()
    {
        await using var context = CreateContext();
        var owner = SeedStudent(context, "download-owner@example.com");
        var other = SeedStudent(context, "download-other@example.com");
        var category = new Category { Id = Guid.NewGuid(), Name = "Downloads" };
        var task = new StudentWorkforceManagement.Domain.Entities.Task { Id = Guid.NewGuid(), Title = "Download", CategoryId = category.Id, CreatedById = Guid.NewGuid(), AssignedStudentId = owner.Id, Priority = TaskPriority.MEDIUM, Difficulty = TaskDifficulty.EASY, Status = TaskStatus.SUBMITTED_FOR_REVIEW, Deadline = DateTimeOffset.UtcNow.AddDays(2), EstimatedDurationMinutes = 60 };
        var submission = new TaskSubmission { Id = Guid.NewGuid(), TaskId = task.Id, SubmittedById = owner.Id, Status = SubmissionStatus.SUBMITTED_FOR_REVIEW };
        var version = new SubmissionVersion
        {
            Id = Guid.NewGuid(),
            TaskSubmissionId = submission.Id,
            TaskSubmission = submission,
            VersionNumber = 1,
            UploadedById = owner.Id,
            UploadedAt = DateTimeOffset.UtcNow,
            FileStatus = FileStatus.CONFIRMED,
            File = new StudentWorkforceManagement.Domain.ValueObjects.FileMetadata { FileName = "deliverable.pdf", StorageKey = "task-submissions/file.pdf", FileSize = 12, MimeType = "application/pdf", FileExtension = ".pdf" }
        };
        context.Categories.Add(category);
        context.Tasks.Add(task);
        context.TaskSubmissions.Add(submission);
        context.SubmissionVersions.Add(version);
        await context.SaveChangesAsync();
        var ownerHandler = new GetSubmissionQueryHandler(context, new FakeCurrentUser(Guid.NewGuid(), owner.Id, UserRole.STUDENT), new FakeFileStorage());
        var otherHandler = new GetSubmissionQueryHandler(context, new FakeCurrentUser(Guid.NewGuid(), other.Id, UserRole.STUDENT), new FakeFileStorage());

        var download = await ownerHandler.Handle(new GetSubmissionVersionDownloadUrlQuery(version.Id, submission.Id), CancellationToken.None);

        Assert.Equal(version.Id, download.SubmissionVersionId);
        Assert.True(download.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.DoesNotContain("SecretKey", download.SignedDownloadUrl.ToString(), StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<ForbiddenException>(() => otherHandler.Handle(new GetSubmissionVersionDownloadUrlQuery(version.Id, submission.Id), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => ownerHandler.Handle(new GetSubmissionVersionDownloadUrlQuery(version.Id, Guid.NewGuid()), CancellationToken.None));
    }

    private static IUploadFilePolicy CreateUploadPolicy()
    {
        return new UploadFilePolicy(Options.Create(new UploadFilePolicyOptions()));
    }

    [Fact]
    public async System.Threading.Tasks.Task Analytics_dashboard_uses_database_counts()
    {
        await using var context = CreateContext();
        var category = new Category { Id = Guid.NewGuid(), Name = "Ops" };
        context.Categories.Add(category);
        context.Tasks.Add(new StudentWorkforceManagement.Domain.Entities.Task { Id = Guid.NewGuid(), Title = "Done", CategoryId = category.Id, CreatedById = Guid.NewGuid(), Priority = TaskPriority.MEDIUM, Difficulty = TaskDifficulty.EASY, Status = TaskStatus.COMPLETED, Deadline = DateTimeOffset.UtcNow.AddDays(-1), EstimatedDurationMinutes = 20 });
        context.Tasks.Add(new StudentWorkforceManagement.Domain.Entities.Task { Id = Guid.NewGuid(), Title = "Late", CategoryId = category.Id, CreatedById = Guid.NewGuid(), Priority = TaskPriority.MEDIUM, Difficulty = TaskDifficulty.EASY, Status = TaskStatus.IN_PROGRESS, Deadline = DateTimeOffset.UtcNow.AddDays(-1), EstimatedDurationMinutes = 30 });
        context.TaskRequests.Add(new TaskRequest { Id = Guid.NewGuid(), TaskId = context.Tasks.Local.Last().Id, RequestedById = Guid.NewGuid(), Type = RequestType.EXTENSION, Status = RequestStatus.PENDING, Reason = "Need time" });
        await context.SaveChangesAsync();
        var handler = new AnalyticsQueryHandler(context, new FakeClock(DateTimeOffset.UtcNow));

        var dashboard = await handler.Handle(new GetDashboardAnalyticsQuery(), CancellationToken.None);

        Assert.Equal(2, dashboard.TotalTasks);
        Assert.Equal(1, dashboard.CompletedTasks);
        Assert.Equal(1, dashboard.OverdueTasks);
        Assert.Equal(1, dashboard.PendingRequests);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Student SeedStudent(ApplicationDbContext context, string email)
    {
        var student = new Student { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), FirstName = "Ada", LastName = "Lovelace", Email = email, Department = "Computer Engineering", IsActive = true };
        context.Students.Add(student);
        return student;
    }

    private sealed class FakeCurrentUser(Guid userId, Guid? studentId, params UserRole[] roles) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
        public Guid? StudentId { get; } = studentId;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<UserRole> Roles { get; } = roles;
    }

    private sealed class FakeClock(DateTimeOffset? now = null) : IUtcClock
    {
        public DateTimeOffset UtcNow { get; } = now ?? DateTimeOffset.UtcNow;
    }

    private sealed class FakeTokenGenerator(string token) : ISecureTokenGenerator
    {
        public string GenerateToken() => token;
        public string HashToken(string rawToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public EmailMessage? LastMessage { get; private set; }
        public System.Threading.Tasks.Task QueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            LastMessage = message;
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }


    private sealed class FakePasswordService : IPasswordService
    {
        public bool VerifyPassword(User user, string password) => user.PasswordHash == $"hash:{password}";
        public string HashPassword(User user, string password) => $"hash:{password}";
    }

    private sealed class FakeAccessTokenService : IAccessTokenService
    {
        public AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> roles, Guid sessionId) => new("access-token", DateTimeOffset.UtcNow.AddMinutes(15));
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Dictionary<string, StoredFileMetadata> Metadata { get; } = new(StringComparer.Ordinal);
        public System.Threading.Tasks.Task<SignedUploadTarget> CreateUploadTargetAsync(UploadTargetRequest request, CancellationToken cancellationToken = default)
        {
            var storageKey = $"{request.OwnershipScope}/{Guid.NewGuid():N}{request.FileExtension}";
            return System.Threading.Tasks.Task.FromResult(new SignedUploadTarget(Guid.NewGuid(), storageKey, new Uri($"https://storage.example/uploads/{storageKey}", UriKind.Absolute), DateTimeOffset.UtcNow.AddMinutes(5), false, "PUT", new Dictionary<string, string> { ["Content-Type"] = request.MimeType }));
        }
        public System.Threading.Tasks.Task<SignedDownloadTarget> CreateDownloadTargetAsync(string storageKey, CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.FromResult(new SignedDownloadTarget(new Uri("https://storage.example/download"), DateTimeOffset.UtcNow.AddMinutes(5)));
        public System.Threading.Tasks.Task<StoredFileMetadata?> GetMetadataAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Metadata.TryGetValue(storageKey, out var value);
            return System.Threading.Tasks.Task.FromResult(value);
        }

        public System.Threading.Tasks.Task SaveAsync(string storageKey, Stream content, string mimeType, CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;

        public System.Threading.Tasks.Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.FromResult<Stream>(new MemoryStream());
    }
}
