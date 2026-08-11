using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Email;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.EmailDispatch;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.OverdueTasks;
using StudentWorkforceManagement.Infrastructure.Email;
using StudentWorkforceManagement.Infrastructure.Email.Delivery;
using StudentWorkforceManagement.Infrastructure.Email.Providers;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Infrastructure.Security.Password;
using StudentWorkforceManagement.Infrastructure.Security.Tokens;
using StudentWorkforceManagement.Infrastructure.Storage;
using StudentWorkforceManagement.Infrastructure.Storage.Local;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;
using DomainTaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.IntegrationTests.Infrastructure;

public sealed class InfrastructureProviderTests
{
    [Fact]
    public void Identity_password_service_hashes_and_verifies_without_plaintext_storage()
    {
        var service = new IdentityPasswordService(new PasswordHasher<User>());
        var user = new User { Id = Guid.NewGuid(), Email = "auth@example.com", DisplayName = "Auth User" };

        var hash = service.HashPassword(user, "Password1");
        user.PasswordHash = hash;

        Assert.NotEqual("Password1", hash);
        Assert.True(service.VerifyPassword(user, "Password1"));
        Assert.False(service.VerifyPassword(user, "wrong-password"));
    }

    [Fact]
    public void Secure_token_generator_creates_unpredictable_raw_tokens_and_deterministic_hashes()
    {
        var generator = new SecureTokenGenerator();

        var first = generator.GenerateToken();
        var second = generator.GenerateToken();

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("+", first, StringComparison.Ordinal);
        Assert.DoesNotContain("/", first, StringComparison.Ordinal);
        Assert.Equal(generator.HashToken(first), generator.HashToken(first));
        Assert.NotEqual(first, generator.HashToken(first));
    }

    [Fact]
    public async System.Threading.Tasks.Task Local_file_storage_generates_opaque_keys_and_blocks_traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(Options.Create(new StorageOptions { Provider = "Local", LocalRootPath = root, SignedUrlLifetimeMinutes = 15 }));

        var target = await storage.CreateUploadTargetAsync(new UploadTargetRequest("report.pdf", 100, "application/pdf", ".pdf", "department-files", false));

        Assert.StartsWith("department-files/", target.StorageKey);
        Assert.DoesNotContain("report", target.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.True(target.UploadUrl.IsAbsoluteUri == false);
        Assert.Throws<InvalidOperationException>(() => storage.ResolvePath("../secret.txt"));
        Assert.Throws<InvalidOperationException>(() => storage.ResolvePath("/etc/passwd"));
        Assert.Throws<InvalidOperationException>(() => storage.ResolvePath("folder\\secret.txt"));
    }

    [Fact]
    public async System.Threading.Tasks.Task Email_service_queues_durable_payload_and_dispatch_job_is_idempotent()
    {
        await using var context = CreateContext();
        var secretProtector = new FakeEmailSecretProtector();
        var emailService = new EmailService(context, secretProtector);
        var message = new EmailMessage(
            "ada@example.com",
            "Welcome",
            "auth.invitation",
            new Dictionary<string, string> { ["name"] = "Ada" },
            "invite:1",
            new Dictionary<string, string> { ["invitationToken"] = "raw-invite-token" });

        await emailService.QueueAsync(message);
        await emailService.QueueAsync(message);
        await context.SaveChangesAsync();

        var delivery = await context.EmailDeliveries.SingleAsync();
        Assert.Equal("Welcome", delivery.Subject);
        Assert.Contains("Ada", delivery.TemplateDataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-invite-token", delivery.TemplateDataJson, StringComparison.Ordinal);
        Assert.Contains("__protected:invitationToken", delivery.TemplateDataJson, StringComparison.Ordinal);

        var provider = new CapturingEmailProvider();
        var job = new EmailDispatchJob(context, provider, new EmailMessageFactory(secretProtector), new FakeClock(), Options.Create(new EmailOptions { Provider = "Development", BatchSize = 10, MaxAttempts = 3, BaseRetryDelayMinutes = 1 }), NullLogger<EmailDispatchJob>.Instance);

        var sent = await job.RunAsync();
        var sentAgain = await job.RunAsync();

        Assert.Equal(1, sent);
        Assert.Equal(0, sentAgain);
        Assert.Single(provider.Sent);
        Assert.Equal("raw-invite-token", provider.Sent.Single().SecretTemplateData?["invitationToken"]);
        Assert.DoesNotContain("__protected:invitationToken", delivery.TemplateDataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-invite-token", delivery.TemplateDataJson, StringComparison.Ordinal);
        Assert.Equal(EmailDeliveryStatus.SENT, delivery.Status);
        Assert.NotNull(delivery.SentAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task Persisted_data_protection_key_ring_decrypts_email_secret_after_service_rebuild()
    {
        var keyRingPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        await using var context = CreateContext();
        var firstProtector = CreateDataProtectionEmailSecretProtector(keyRingPath);
        var emailService = new EmailService(context, firstProtector);

        await emailService.QueueAsync(new EmailMessage(
            "ada@example.com",
            "Welcome",
            "auth.invitation",
            new Dictionary<string, string> { ["name"] = "Ada" },
            "invite:shared-keyring",
            new Dictionary<string, string> { ["invitationToken"] = "restart-token" }));
        await context.SaveChangesAsync();
        var delivery = await context.EmailDeliveries.SingleAsync();

        Assert.DoesNotContain("restart-token", delivery.TemplateDataJson, StringComparison.Ordinal);

        var rebuiltProtector = CreateDataProtectionEmailSecretProtector(keyRingPath);
        var message = new EmailMessageFactory(rebuiltProtector).FromDelivery(delivery);

        Assert.Equal("restart-token", message.SecretTemplateData?["invitationToken"]);
    }

    [Fact]
    public async System.Threading.Tasks.Task Email_dispatch_fails_safely_when_data_protection_key_ring_is_missing()
    {
        await using var context = CreateContext();
        var originalKeyRingPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        var wrongKeyRingPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        var emailService = new EmailService(context, CreateDataProtectionEmailSecretProtector(originalKeyRingPath));
        await emailService.QueueAsync(new EmailMessage(
            "ada@example.com",
            "Welcome",
            "auth.invitation",
            new Dictionary<string, string>(),
            "invite:wrong-keyring",
            new Dictionary<string, string> { ["invitationToken"] = "missing-key-token" }));
        await context.SaveChangesAsync();

        var provider = new CapturingEmailProvider();
        var job = new EmailDispatchJob(
            context,
            provider,
            new EmailMessageFactory(CreateDataProtectionEmailSecretProtector(wrongKeyRingPath)),
            new FakeClock(),
            Options.Create(new EmailOptions { Provider = "Development", BatchSize = 10, MaxAttempts = 1, BaseRetryDelayMinutes = 1 }),
            NullLogger<EmailDispatchJob>.Instance);

        var sent = await job.RunAsync();
        var delivery = await context.EmailDeliveries.SingleAsync();

        Assert.Equal(0, sent);
        Assert.Empty(provider.Sent);
        Assert.Equal(EmailDeliveryStatus.FAILED, delivery.Status);
        Assert.Equal("Email delivery failed.", delivery.FailureReason);
        Assert.DoesNotContain("missing-key-token", delivery.TemplateDataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task Development_email_provider_captures_reset_message_in_local_sink_only()
    {
        var sinkPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var provider = new DevelopmentEmailProvider(
            new FakeHostEnvironment("Development"),
            Options.Create(new EmailOptions
            {
                Provider = "Development",
                DevelopmentSinkPath = sinkPath,
                DevelopmentFrontendBaseUrl = "http://localhost:5173"
            }),
            new EmailTemplateRenderer(),
            NullLogger<DevelopmentEmailProvider>.Instance);
        var message = new EmailMessage(
            "ada@example.com",
            "Reset your password",
            "auth.password-reset",
            new Dictionary<string, string> { ["userId"] = Guid.NewGuid().ToString("N") },
            "password-reset:1",
            new Dictionary<string, string> { ["resetToken"] = "raw-reset-token" });

        var result = await provider.SendAsync(message);

        Assert.True(result.Accepted);
        var latestResetMessage = await File.ReadAllTextAsync(Path.Combine(sinkPath, "latest-auth.password-reset.json"));
        Assert.Contains("http://localhost:5173/reset-password?token=raw-reset-token", latestResetMessage, StringComparison.Ordinal);
        Assert.Contains("raw-reset-token", latestResetMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task Development_email_provider_cannot_be_used_outside_development()
    {
        var sinkPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var provider = new DevelopmentEmailProvider(
            new FakeHostEnvironment("Production"),
            Options.Create(new EmailOptions { Provider = "Development", DevelopmentSinkPath = sinkPath }),
            new EmailTemplateRenderer(),
            NullLogger<DevelopmentEmailProvider>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.SendAsync(new EmailMessage(
            "ada@example.com",
            "Reset your password",
            "auth.password-reset",
            new Dictionary<string, string>(),
            "password-reset:blocked",
            new Dictionary<string, string> { ["resetToken"] = "raw-reset-token" })));
        Assert.False(Directory.Exists(sinkPath));
    }

    [Fact]
    public async System.Threading.Tasks.Task Overdue_job_marks_due_tasks_once_and_creates_single_notification()
    {
        await using var context = CreateContext();
        var user = new User { Id = Guid.NewGuid(), Email = "student@example.com", DisplayName = "Student", IsActive = true };
        var student = new Student { Id = Guid.NewGuid(), UserId = user.Id, User = user, FirstName = "Ada", LastName = "Lovelace", Email = user.Email, Department = "Computer Engineering", IsActive = true };
        var category = new Category { Id = Guid.NewGuid(), Name = "Ops" };
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = "Late task",
            CategoryId = category.Id,
            Category = category,
            CreatedById = Guid.NewGuid(),
            AssignedStudentId = student.Id,
            AssignedStudent = student,
            Status = DomainTaskStatus.IN_PROGRESS,
            Priority = TaskPriority.MEDIUM,
            Difficulty = TaskDifficulty.EASY,
            Deadline = DateTimeOffset.UtcNow.AddHours(-1),
            EstimatedDurationMinutes = 30
        };
        context.Users.Add(user);
        context.Students.Add(student);
        context.Categories.Add(category);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var job = new OverdueTaskJob(context, new NotificationIntentService(context), new FakeClock(), Options.Create(new BackgroundJobOptions { BatchSize = 10 }), NullLogger<OverdueTaskJob>.Instance);

        var firstRun = await job.RunAsync();
        var secondRun = await job.RunAsync();

        Assert.Equal(1, firstRun);
        Assert.Equal(0, secondRun);
        Assert.Equal(DomainTaskStatus.OVERDUE, task.Status);
        Assert.Single(context.Notifications);
        Assert.Equal($"TASK_{task.Id:N}_OVERDUE", context.Notifications.Single().IdempotencyKey);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DataProtectionEmailSecretProtector CreateDataProtectionEmailSecretProtector(string keyRingPath)
    {
        var provider = DataProtectionProvider.Create(new DirectoryInfo(keyRingPath), builder => builder.SetApplicationName("StudentWorkforceManagement.Tests"));
        return new DataProtectionEmailSecretProtector(provider);
    }

    private sealed class FakeClock(DateTimeOffset? now = null) : IUtcClock
    {
        public DateTimeOffset UtcNow { get; } = now ?? DateTimeOffset.UtcNow;
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "StudentWorkforceManagement.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CapturingEmailProvider : IEmailProvider
    {
        public List<EmailMessage> Sent { get; } = [];

        public System.Threading.Tasks.Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return System.Threading.Tasks.Task.FromResult(new EmailProviderResult(true, message.IdempotencyKey, null));
        }
    }

    private sealed class FakeEmailSecretProtector : IEmailSecretProtector
    {
        public string Protect(string secret) => $"protected::{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(secret))}";

        public string Unprotect(string protectedSecret)
        {
            var payload = protectedSecret["protected::".Length..];
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        }
    }
}
