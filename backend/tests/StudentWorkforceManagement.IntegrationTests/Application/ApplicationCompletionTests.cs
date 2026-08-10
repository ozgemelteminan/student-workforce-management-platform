using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
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
using StudentWorkforceManagement.Application.Students.Queries;
using StudentWorkforceManagement.Application.Submissions.Commands.CompleteSubmissionUpload;
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
        Assert.Equal("raw-invite-token", email.LastMessage?.TemplateData["invitationToken"]);

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
        Assert.Equal("reset-token", email.LastMessage?.TemplateData["resetToken"]);

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
        var handler = new SessionCommandHandler(context, new FakeCurrentUser(userId, null, UserRole.STUDENT), tokenGenerator, new FakeClock());

        var rotated = await handler.Handle(new RefreshTokenCommand("old-refresh-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(session.Id, rotated.SessionId);
        Assert.Equal(2, context.RefreshTokens.Count());
        Assert.NotNull(context.RefreshTokens.Single(token => token.TokenHash == tokenGenerator.HashToken("old-refresh-token")).ReplacedAt);
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new RefreshTokenCommand("old-refresh-token", DateTimeOffset.UtcNow.AddDays(8)), CancellationToken.None));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new LogoutCommand(context.Sessions.Single(item => item.UserId == otherUserId).Id), CancellationToken.None));
        await handler.Handle(new LogoutCommand(session.Id), CancellationToken.None);
        Assert.NotNull(session.RevokedAt);
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
        var handler = new FileCommandHandler(context, new FakeCurrentUser(Guid.NewGuid(), null, UserRole.ADMIN), storage, new FakeClock());

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
        var handler = new CompleteSubmissionUploadCommandHandler(context, new FakeCurrentUser(Guid.NewGuid(), student.Id, UserRole.STUDENT), storage, new FakeClock());

        var initiated = await handler.Handle(new InitiateSubmissionUploadCommand(task.Id, "deliverable.pdf", 2048, "application/pdf", ".pdf", "hash-1"), CancellationToken.None);
        await context.SaveChangesAsync();
        storage.Metadata[initiated.StorageKey] = new StoredFileMetadata(initiated.StorageKey, 2048, "application/pdf", "hash-1");

        Assert.StartsWith("task-submissions/", initiated.StorageKey);
        Assert.DoesNotContain("deliverable", initiated.StorageKey, StringComparison.OrdinalIgnoreCase);

        var completed = await handler.Handle(new CompleteSubmissionUploadCommand(initiated.SubmissionVersionId), CancellationToken.None);
        var completedAgain = await handler.Handle(new CompleteSubmissionUploadCommand(initiated.SubmissionVersionId), CancellationToken.None);

        Assert.Equal(FileStatus.CONFIRMED, completed.FileStatus);
        Assert.Equal(completed.Id, completedAgain.Id);
        Assert.Single(context.SubmissionVersions);
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
        public string CreateAccessToken(User user, IReadOnlyCollection<string> roles, Guid sessionId) => "access-token";
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Dictionary<string, StoredFileMetadata> Metadata { get; } = new(StringComparer.Ordinal);
        public System.Threading.Tasks.Task<SignedUploadTarget> CreateUploadTargetAsync(UploadTargetRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public System.Threading.Tasks.Task<SignedDownloadTarget> CreateDownloadTargetAsync(string storageKey, CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.FromResult(new SignedDownloadTarget(new Uri("https://storage.example/download"), DateTimeOffset.UtcNow.AddMinutes(5)));
        public System.Threading.Tasks.Task<StoredFileMetadata?> GetMetadataAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Metadata.TryGetValue(storageKey, out var value);
            return System.Threading.Tasks.Task.FromResult(value);
        }
    }
}
