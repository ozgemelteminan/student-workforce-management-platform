using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.DataExport;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Infrastructure.Security.Tokens;
using Category = StudentWorkforceManagement.Domain.Entities.Category;
using Role = StudentWorkforceManagement.Domain.Entities.Role;
using Session = StudentWorkforceManagement.Domain.Entities.Session;
using Student = StudentWorkforceManagement.Domain.Entities.Student;
using SubmissionVersion = StudentWorkforceManagement.Domain.Entities.SubmissionVersion;
using TaskSubmission = StudentWorkforceManagement.Domain.Entities.TaskSubmission;
using TaskEntity = StudentWorkforceManagement.Domain.Entities.Task;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;
using User = StudentWorkforceManagement.Domain.Entities.User;

namespace StudentWorkforceManagement.IntegrationTests.Api;

public sealed class LiveApiClosureTests
{
    private const string SigningKey = "TEST_ONLY_SIGNING_KEY_0123456789_32_CHARS_MIN";
    private const string Issuer = "StudentWorkforceManagement.Tests";
    private const string Audience = "StudentWorkforceManagement.Api.Tests";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async System.Threading.Tasks.Task Valid_real_jwt_reaches_authorized_endpoint()
    {
        await using var factory = await TestApiFactory.CreateInitializedAsync();
        using var client = factory.CreateAuthenticatedClient(factory.Seed.AdminUserId, factory.Seed.AdminSessionId);

        var response = await client.GetAsync("/api/v1/skills");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task Invalid_tokens_and_session_state_return_401()
    {
        await using var factory = await TestApiFactory.CreateInitializedAsync();

        using var malformed = factory.CreateClient();
        malformed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, (await malformed.GetAsync("/api/v1/skills")).StatusCode);

        using var invalidSignature = factory.CreateClient();
        invalidSignature.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(factory.Seed.AdminUserId, factory.Seed.AdminSessionId, signingKey: "WRONG_TEST_SIGNING_KEY_0123456789_32_CHARS"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await invalidSignature.GetAsync("/api/v1/skills")).StatusCode);

        using var expired = factory.CreateClient();
        expired.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(factory.Seed.AdminUserId, factory.Seed.AdminSessionId, accessTokenMinutes: -5));
        Assert.Equal(HttpStatusCode.Unauthorized, (await expired.GetAsync("/api/v1/skills")).StatusCode);

        using var revoked = factory.CreateClient();
        revoked.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(factory.Seed.AdminUserId, factory.Seed.RevokedSessionId));
        Assert.Equal(HttpStatusCode.Unauthorized, (await revoked.GetAsync("/api/v1/skills")).StatusCode);

        using var inactive = factory.CreateClient();
        inactive.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(factory.Seed.InactiveUserId, factory.Seed.InactiveSessionId));
        Assert.Equal(HttpStatusCode.Unauthorized, (await inactive.GetAsync("/api/v1/skills")).StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task Role_enforcement_and_idor_boundaries_are_enforced_over_http()
    {
        await using var factory = await TestApiFactory.CreateInitializedAsync();

        using var admin = factory.CreateAuthenticatedClient(factory.Seed.AdminUserId, factory.Seed.AdminSessionId);
        var adminResponse = await admin.GetAsync("/api/v1/audit");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        using var student = factory.CreateAuthenticatedClient(factory.Seed.StudentUserId, factory.Seed.StudentSessionId);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/v1/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/v1/students/{factory.Seed.OtherStudentId:D}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/v1/tasks/{factory.Seed.OtherStudentTaskId:D}")).StatusCode);

        using var manager = factory.CreateAuthenticatedClient(factory.Seed.TaskManagerUserId, factory.Seed.TaskManagerSessionId);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/v1/submissions/{factory.Seed.ReviewableSubmissionId:D}/approve", new { reviewerComment = "ok" })).StatusCode);

        using var reviewer = factory.CreateAuthenticatedClient(factory.Seed.ReviewerUserId, factory.Seed.ReviewerSessionId);
        var reviewResponse = await reviewer.PostAsJsonAsync($"/api/v1/submissions/{factory.Seed.ReviewableSubmissionId:D}/approve", new { reviewerComment = "approved" });
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task Durable_export_lifecycle_blocks_idor_and_streams_completed_artifacts()
    {
        await using var factory = await TestApiFactory.CreateInitializedAsync();
        using var student = factory.CreateAuthenticatedClient(factory.Seed.StudentUserId, factory.Seed.StudentSessionId);

        var create = await student.PostAsJsonAsync("/api/v1/exports", new { type = "PersonalData", format = "Csv" });
        Assert.Equal(HttpStatusCode.Accepted, create.StatusCode);
        var created = await ReadJsonAsync(create);
        var id = created.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("QUEUED", created.RootElement.GetProperty("status").GetString());

        var list = await student.GetAsync("/api/v1/exports");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(id.ToString("D"), await list.Content.ReadAsStringAsync());

        using var otherStudent = factory.CreateAuthenticatedClient(factory.Seed.OtherStudentUserId, factory.Seed.OtherStudentSessionId);
        var idor = await otherStudent.GetAsync($"/api/v1/exports/{id:D}");
        Assert.Equal(HttpStatusCode.NotFound, idor.StatusCode);

        await factory.RunExportJobAsync(id);

        var status = await student.GetAsync($"/api/v1/exports/{id:D}");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Contains("COMPLETED", await status.Content.ReadAsStringAsync());

        var download = await student.GetAsync($"/api/v1/exports/{id:D}/download", HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("text/csv", download.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("file://", download.RequestMessage?.RequestUri?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async System.Threading.Tasks.Task File_api_direct_upload_contract_is_enforced_over_http()
    {
        await using var factory = await TestApiFactory.CreateInitializedAsync();
        using var manager = factory.CreateAuthenticatedClient(factory.Seed.TaskManagerUserId, factory.Seed.TaskManagerSessionId);

        var oversized = await manager.PostAsJsonAsync("/api/v1/files/uploads", new { fileName = "too-big.csv", fileSize = 1_073_741_825L, mimeType = "text/csv", fileExtension = ".csv", storageKey = "../client-supplied" });
        Assert.Equal((HttpStatusCode)422, oversized.StatusCode);

        var accepted = await manager.PostAsJsonAsync("/api/v1/files/uploads", new { fileName = "max.csv", fileSize = 1_073_741_824L, mimeType = "text/csv", fileExtension = ".csv", storageKey = "../client-supplied" });
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        var json = await ReadJsonAsync(accepted);
        var fileId = json.RootElement.GetProperty("fileId").GetGuid();
        var storageKey = json.RootElement.GetProperty("storageKey").GetString()!;
        Assert.StartsWith("department-files/", storageKey, StringComparison.Ordinal);
        Assert.DoesNotContain("client-supplied", storageKey, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("..", storageKey, StringComparison.Ordinal);

        factory.Storage.Metadata[storageKey] = new StoredFileMetadata(storageKey, 1_073_741_824L, "text/csv", null);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"/api/v1/files/{fileId:D}/complete", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync($"/api/v1/files/{fileId:D}/complete", null)).StatusCode);

        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v1/files/{fileId:D}/download")).StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task ProblemDetails_and_rate_limiting_are_returned_by_runtime_pipeline()
    {
        await using var factory = await TestApiFactory.CreateInitializedAsync();
        using var admin = factory.CreateAuthenticatedClient(factory.Seed.AdminUserId, factory.Seed.AdminSessionId);

        var validation = await admin.PostAsJsonAsync("/api/v1/tasks", new { title = "", categoryId = Guid.Empty, priority = "MEDIUM", difficulty = "EASY", deadline = DateTimeOffset.UtcNow, estimatedDurationMinutes = 0 });
        Assert.Equal((HttpStatusCode)422, validation.StatusCode);
        await AssertSafeProblemAsync(validation);

        var notFound = await admin.GetAsync($"/api/v1/tasks/{Guid.NewGuid():D}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        await AssertSafeProblemAsync(notFound);

        var conflictCreate = await admin.PostAsJsonAsync("/api/v1/files/uploads", new { fileName = "pending.csv", fileSize = 10L, mimeType = "text/csv", fileExtension = ".csv" });
        var conflictId = (await ReadJsonAsync(conflictCreate)).RootElement.GetProperty("fileId").GetGuid();
        var conflict = await admin.PostAsync($"/api/v1/files/{conflictId:D}/complete", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        await AssertSafeProblemAsync(conflict);

        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/skills")).StatusCode);

        HttpResponseMessage? loginResponse = null;
        for (var i = 0; i < 6; i++)
        {
            loginResponse = await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = "student@example.edu", password = "wrong", deviceName = "test" });
        }
        Assert.Equal((HttpStatusCode)429, loginResponse!.StatusCode);
        await AssertSafeProblemAsync(loginResponse);

        HttpResponseMessage? forgotResponse = null;
        for (var i = 0; i < 4; i++)
        {
            forgotResponse = await anonymous.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "student@example.edu" });
        }
        Assert.Equal((HttpStatusCode)429, forgotResponse!.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task Runtime_openapi_signalr_hangfire_and_cors_security_are_verified()
    {
        await using var factory = await TestApiFactory.CreateInitializedAsync();

        using var anonymous = factory.CreateClient();
        var openApi = await anonymous.GetStringAsync("/openapi/v1.json");
        Assert.Contains("\"bearerAuth\"", openApi);
        Assert.Contains("\"/api/v1/tasks/{id}/start\"", openApi);
        Assert.Contains("IN_PROGRESS", openApi);
        using var openApiJson = JsonDocument.Parse(openApi);
        Assert.True(ContainsStringEnumValue(openApiJson.RootElement, "IN_PROGRESS"));
        Assert.DoesNotContain("/api/v1/v2", openApi);
        Assert.DoesNotContain("501", openApi);

        var queryTokenIgnored = await anonymous.GetAsync($"/api/v1/skills?access_token={factory.CreateToken(factory.Seed.AdminUserId, factory.Seed.AdminSessionId)}");
        Assert.Equal(HttpStatusCode.Unauthorized, queryTokenIgnored.StatusCode);

        var negotiate = await anonymous.PostAsync($"/hubs/notifications/negotiate?negotiateVersion=1&access_token={factory.CreateToken(factory.Seed.AdminUserId, factory.Seed.AdminSessionId)}", null);
        Assert.Equal(HttpStatusCode.OK, negotiate.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/admin/hangfire")).StatusCode);

        using var student = factory.CreateAuthenticatedClient(factory.Seed.StudentUserId, factory.Seed.StudentSessionId);
        var nonAdminHangfire = await student.GetAsync("/admin/hangfire");
        Assert.True(nonAdminHangfire.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);

        using var admin = factory.CreateAuthenticatedClient(factory.Seed.AdminUserId, factory.Seed.AdminSessionId);
        var adminHangfire = await admin.GetAsync("/admin/hangfire");
        Assert.False(adminHangfire.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/skills");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        var cors = await anonymous.SendAsync(request);
        Assert.Equal("http://localhost:5173", cors.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.True(cors.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static async System.Threading.Tasks.Task AssertSafeProblemAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("traceId", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storage/local", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DEV_ONLY_SIGNING", body, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsStringEnumValue(JsonElement element, string value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("enum", out var enumValues)
                && enumValues.ValueKind == JsonValueKind.Array
                && enumValues.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String && item.GetString() == value))
            {
                return element.TryGetProperty("type", out var type)
                    && type.ValueKind == JsonValueKind.String
                    && type.GetString() == "string";
            }

            return element.EnumerateObject().Any(property => ContainsStringEnumValue(property.Value, value));
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Any(item => ContainsStringEnumValue(item, value));
        }

        return false;
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"api-tests-{Guid.NewGuid():N}";
        private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), $"swm-api-tests-{Guid.NewGuid():N}");

        public TestSeed Seed { get; private set; } = null!;
        public FakeFileStorage Storage { get; } = new();
        public FakeExportJobScheduler Scheduler { get; } = new();

        public static async Task<TestApiFactory> CreateInitializedAsync()
        {
            var factory = new TestApiFactory();
            _ = factory.CreateClient();
            await factory.SeedAsync();
            return factory;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:SigningKey"] = SigningKey,
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["Email:Provider"] = "Development",
                    ["Storage:Provider"] = "Local",
                    ["Storage:LocalRootPath"] = _storageRoot,
                    ["BackgroundJobs:EnableServer"] = "false",
                    ["DataProtection:KeysPath"] = Path.Combine(_storageRoot, "keys"),
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                    ["Exports:ArtifactExpirationHours"] = "24"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                var inMemoryServices = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();
                services.AddDbContext<ApplicationDbContext>(options => options
                    .UseInMemoryDatabase(_databaseName)
                    .UseInternalServiceProvider(inMemoryServices)
                    .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
                services.RemoveAll<IApplicationDbContext>();
                services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());

                services.RemoveAll<IFileStorage>();
                services.AddSingleton<IFileStorage>(Storage);
                services.RemoveAll<IExportJobScheduler>();
                services.AddSingleton<IExportJobScheduler>(Scheduler);
                services.RemoveAll<IHostedService>();
            });
        }

        public HttpClient CreateAuthenticatedClient(Guid userId, Guid sessionId)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(userId, sessionId));
            return client;
        }

        public string CreateToken(Guid userId, Guid sessionId, string signingKey = SigningKey, int accessTokenMinutes = 15)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = dbContext.Users.Include(entity => entity.Role).Include(entity => entity.Student).Single(entity => entity.Id == userId);
            var tokenService = new JwtAccessTokenService(Options.Create(new JwtOptions
            {
                Issuer = Issuer,
                Audience = Audience,
                SigningKey = signingKey,
                AccessTokenMinutes = accessTokenMinutes
            }));
            return tokenService.CreateAccessToken(user, new[] { user.Role!.Name.ToString() }, sessionId);
        }

        public async Task RunExportJobAsync(Guid exportRequestId)
        {
            using var scope = Services.CreateScope();
            var job = ActivatorUtilities.CreateInstance<DataExportJob>(
                scope.ServiceProvider,
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                Storage,
                Options.Create(new DataExportOptions { ArtifactExpirationHours = 24 }),
                scope.ServiceProvider.GetRequiredService<ILogger<DataExportJob>>());
            await job.RunAsync(exportRequestId);
        }

        private async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var adminRole = new Role { Id = Guid.NewGuid(), Name = UserRole.ADMIN };
            var managerRole = new Role { Id = Guid.NewGuid(), Name = UserRole.TASK_MANAGER };
            var reviewerRole = new Role { Id = Guid.NewGuid(), Name = UserRole.REVIEWER };
            var studentRole = new Role { Id = Guid.NewGuid(), Name = UserRole.STUDENT };
            dbContext.Roles.AddRange(adminRole, managerRole, reviewerRole, studentRole);

            var admin = NewUser("admin@example.edu", adminRole);
            var manager = NewUser("manager@example.edu", managerRole);
            var reviewer = NewUser("reviewer@example.edu", reviewerRole);
            var studentUser = NewUser("student@example.edu", studentRole);
            var otherStudentUser = NewUser("other@example.edu", studentRole);
            var inactive = NewUser("inactive@example.edu", studentRole, isActive: false);
            dbContext.Users.AddRange(admin, manager, reviewer, studentUser, otherStudentUser, inactive);

            var student = new Student { Id = Guid.NewGuid(), UserId = studentUser.Id, FirstName = "Test", LastName = "Student", Email = studentUser.Email, Department = "Computer Science", IsActive = true };
            var otherStudent = new Student { Id = Guid.NewGuid(), UserId = otherStudentUser.Id, FirstName = "Other", LastName = "Student", Email = otherStudentUser.Email, Department = "Computer Science", IsActive = true };
            dbContext.Students.AddRange(student, otherStudent);

            var category = new Category { Id = Guid.NewGuid(), Name = "General" };
            dbContext.Categories.Add(category);

            var ownTask = new TaskEntity
            {
                Id = Guid.NewGuid(),
                Title = "Own task",
                CategoryId = category.Id,
                Priority = TaskPriority.MEDIUM,
                Difficulty = TaskDifficulty.EASY,
                Status = TaskStatus.ASSIGNED,
                CreatedById = admin.Id,
                AssignedStudentId = student.Id,
                Deadline = DateTimeOffset.UtcNow.AddDays(7),
                EstimatedDurationMinutes = 60
            };
            var otherTask = new TaskEntity
            {
                Id = Guid.NewGuid(),
                Title = "Other task",
                CategoryId = category.Id,
                Priority = TaskPriority.MEDIUM,
                Difficulty = TaskDifficulty.EASY,
                Status = TaskStatus.ASSIGNED,
                CreatedById = admin.Id,
                AssignedStudentId = otherStudent.Id,
                Deadline = DateTimeOffset.UtcNow.AddDays(7),
                EstimatedDurationMinutes = 60
            };
            var reviewTask = new TaskEntity
            {
                Id = Guid.NewGuid(),
                Title = "Review task",
                CategoryId = category.Id,
                Priority = TaskPriority.MEDIUM,
                Difficulty = TaskDifficulty.EASY,
                Status = TaskStatus.SUBMITTED_FOR_REVIEW,
                CreatedById = admin.Id,
                AssignedStudentId = student.Id,
                Deadline = DateTimeOffset.UtcNow.AddDays(7),
                EstimatedDurationMinutes = 60
            };
            dbContext.Tasks.AddRange(ownTask, otherTask, reviewTask);

            var submission = new TaskSubmission
            {
                Id = Guid.NewGuid(),
                TaskId = reviewTask.Id,
                SubmittedById = student.Id,
                Status = SubmissionStatus.SUBMITTED_FOR_REVIEW,
                SubmittedAt = DateTimeOffset.UtcNow
            };
            dbContext.TaskSubmissions.Add(submission);
            dbContext.SubmissionVersions.Add(new SubmissionVersion
            {
                Id = Guid.NewGuid(),
                TaskSubmissionId = submission.Id,
                VersionNumber = 1,
                File = new FileMetadata { FileName = "submission.txt", StorageKey = "task-submissions/test/submission.txt", FileSize = 10, MimeType = "text/plain", FileExtension = ".txt" },
                FileStatus = FileStatus.CONFIRMED,
                UploadedById = student.Id,
                UploadedAt = DateTimeOffset.UtcNow,
                ConfirmedAt = DateTimeOffset.UtcNow
            });

            var adminSession = NewSession(admin.Id);
            var managerSession = NewSession(manager.Id);
            var reviewerSession = NewSession(reviewer.Id);
            var studentSession = NewSession(studentUser.Id);
            var otherStudentSession = NewSession(otherStudentUser.Id);
            var inactiveSession = NewSession(inactive.Id);
            var revokedSession = NewSession(admin.Id, revoked: true);
            dbContext.Sessions.AddRange(adminSession, managerSession, reviewerSession, studentSession, otherStudentSession, inactiveSession, revokedSession);

            await dbContext.SaveChangesAsync();
            Seed = new TestSeed(admin.Id, adminSession.Id, manager.Id, managerSession.Id, reviewer.Id, reviewerSession.Id, studentUser.Id, studentSession.Id, otherStudentUser.Id, otherStudentSession.Id, inactive.Id, inactiveSession.Id, revokedSession.Id, student.Id, otherStudent.Id, otherTask.Id, submission.Id);
        }

        private static User NewUser(string email, Role role, bool isActive = true)
        {
            var user = new User { Id = Guid.NewGuid(), Email = email, DisplayName = email, RoleId = role.Id, Role = role, IsActive = isActive };
            user.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(user, "Correct1!");
            return user;
        }

        private static Session NewSession(Guid userId, bool revoked = false)
        {
            return new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceName = "test",
                IpAddress = "127.0.0.1",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                RevokedAt = revoked ? DateTimeOffset.UtcNow : null
            };
        }
    }

    private sealed record TestSeed(
        Guid AdminUserId,
        Guid AdminSessionId,
        Guid TaskManagerUserId,
        Guid TaskManagerSessionId,
        Guid ReviewerUserId,
        Guid ReviewerSessionId,
        Guid StudentUserId,
        Guid StudentSessionId,
        Guid OtherStudentUserId,
        Guid OtherStudentSessionId,
        Guid InactiveUserId,
        Guid InactiveSessionId,
        Guid RevokedSessionId,
        Guid StudentId,
        Guid OtherStudentId,
        Guid OtherStudentTaskId,
        Guid ReviewableSubmissionId);

    private sealed class FakeExportJobScheduler : IExportJobScheduler
    {
        public List<Guid> EnqueuedIds { get; } = [];

        public Task EnqueueAsync(Guid exportRequestId, CancellationToken cancellationToken = default)
        {
            EnqueuedIds.Add(exportRequestId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        private readonly Dictionary<string, byte[]> _content = new(StringComparer.Ordinal);
        public Dictionary<string, StoredFileMetadata> Metadata { get; } = new(StringComparer.Ordinal);

        public Task<SignedUploadTarget> CreateUploadTargetAsync(UploadTargetRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Direct upload targets are not used by these HTTP endpoint adapters.");
        }

        public Task<SignedDownloadTarget> CreateDownloadTargetAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SignedDownloadTarget(new Uri($"/api/v1/storage/local/downloads/{Uri.EscapeDataString(storageKey)}", UriKind.Relative), DateTimeOffset.UtcNow.AddMinutes(5)));
        }

        public Task<StoredFileMetadata?> GetMetadataAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Metadata.TryGetValue(storageKey, out var value);
            return Task.FromResult(value);
        }

        public async Task SaveAsync(string storageKey, Stream content, string mimeType, CancellationToken cancellationToken = default)
        {
            using var memory = new MemoryStream();
            await content.CopyToAsync(memory, cancellationToken);
            _content[storageKey] = memory.ToArray();
            Metadata[storageKey] = new StoredFileMetadata(storageKey, memory.Length, mimeType, null);
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream>(new MemoryStream(_content[storageKey], writable: false));
        }
    }
}
