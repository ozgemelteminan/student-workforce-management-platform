using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using StudentWorkforceManagement.Api.Contracts.V1;
using StudentWorkforceManagement.Api.Security;
using StudentWorkforceManagement.Application.Analytics.Queries;
using StudentWorkforceManagement.Application.Announcements.Commands;
using StudentWorkforceManagement.Application.Announcements.Queries.GetAnnouncement;
using StudentWorkforceManagement.Application.Announcements.Queries.GetAnnouncements;
using StudentWorkforceManagement.Application.Audit.Queries;
using StudentWorkforceManagement.Application.Auth.Commands;
using StudentWorkforceManagement.Application.Auth.DTOs;
using StudentWorkforceManagement.Application.Auth.Queries;
using StudentWorkforceManagement.Application.Availability.Commands;
using StudentWorkforceManagement.Application.Availability.Commands.CreateAvailability;
using StudentWorkforceManagement.Application.Availability.Queries.GetAvailability;
using StudentWorkforceManagement.Application.Availability.Queries.GetCurrentSemesterAvailability;
using StudentWorkforceManagement.Application.Categories.Commands.CreateCategory;
using StudentWorkforceManagement.Application.Categories.Queries.GetCategories;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Exports.Commands;
using StudentWorkforceManagement.Application.Feedback.Commands;
using StudentWorkforceManagement.Application.Feedback.Queries;
using StudentWorkforceManagement.Application.Files.Commands;
using StudentWorkforceManagement.Application.Files.Queries;
using StudentWorkforceManagement.Application.Marketplace.Commands;
using StudentWorkforceManagement.Application.Marketplace.Queries.GetMarketplaceListings;
using StudentWorkforceManagement.Application.Notifications.Commands;
using StudentWorkforceManagement.Application.Notifications.Queries.GetNotifications;
using StudentWorkforceManagement.Application.RecurringTasks.Commands;
using StudentWorkforceManagement.Application.RecurringTasks.Queries;
using StudentWorkforceManagement.Application.Requests.Commands.CreateTaskRequest;
using StudentWorkforceManagement.Application.Requests.Commands.ReviewTaskRequest;
using StudentWorkforceManagement.Application.Requests.Queries.GetTaskRequests;
using StudentWorkforceManagement.Application.Schedules.Commands;
using StudentWorkforceManagement.Application.Schedules.Commands.CreateCourseSchedule;
using StudentWorkforceManagement.Application.Schedules.Queries.GetCurrentSemesterSchedule;
using StudentWorkforceManagement.Application.Schedules.Queries.GetStudentSchedule;
using StudentWorkforceManagement.Application.Semesters.Commands;
using StudentWorkforceManagement.Application.Semesters.Queries.GetSemester;
using StudentWorkforceManagement.Application.Semesters.Queries.GetSemesters;
using StudentWorkforceManagement.Application.Settings.Commands.UpdateSetting;
using StudentWorkforceManagement.Application.Settings.Queries.GetSettings;
using StudentWorkforceManagement.Application.Skills.Commands;
using StudentWorkforceManagement.Application.Skills.Queries.GetSkills;
using StudentWorkforceManagement.Application.Students.Commands;
using StudentWorkforceManagement.Application.Students.Queries;
using StudentWorkforceManagement.Application.Submissions.Commands.CompleteSubmissionUpload;
using StudentWorkforceManagement.Application.Submissions.Commands.ReviewSubmission;
using StudentWorkforceManagement.Application.Submissions.Queries.GetSubmission;
using StudentWorkforceManagement.Application.Tasks.Commands.AddTaskDependency;
using StudentWorkforceManagement.Application.Tasks.Commands.AssignTask;
using StudentWorkforceManagement.Application.Tasks.Commands.Checklist;
using StudentWorkforceManagement.Application.Tasks.Commands.Comments;
using StudentWorkforceManagement.Application.Tasks.Commands.CreateTask;
using StudentWorkforceManagement.Application.Tasks.Commands.ReassignTask;
using StudentWorkforceManagement.Application.Tasks.Commands.TransitionTask;
using StudentWorkforceManagement.Application.Tasks.Commands.UnassignTask;
using StudentWorkforceManagement.Application.Tasks.Commands.UpdateTask;
using StudentWorkforceManagement.Application.Tasks.Queries.GetAssignmentRecommendations;
using StudentWorkforceManagement.Application.Tasks.Queries.GetMyTasks;
using StudentWorkforceManagement.Application.Tasks.Queries.GetTask;
using StudentWorkforceManagement.Application.Tasks.Queries.GetTasks;
using StudentWorkforceManagement.Application.Tasks.Queries.GetTaskSubresources;
using StudentWorkforceManagement.Application.Templates.Commands;
using StudentWorkforceManagement.Application.Templates.Queries;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Api.Endpoints;

public static class V1Endpoints
{
    private const int MaxPageSize = 100;
    private static readonly TimeSpan LoginFailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ForgotPasswordWindow = TimeSpan.FromHours(1);

    public static void MapV1Api(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(ApiRoutes.VersionPrefix)
            .WithTags("v1");

        api.MapGet("/", () => Results.Ok(new { version = "v1", status = "api" }))
            .AllowAnonymous()
            .WithName("ApiV1Root");

        MapAuth(api);
        MapCatalog(api);
        MapStudents(api);
        MapTasks(api);
        MapRequests(api);
        MapSubmissions(api);
        MapMarketplace(api);
        MapSchedules(api);
        MapFiles(api);
        MapAnnouncements(api);
        MapNotifications(api);
        MapFeedback(api);
        MapTemplates(api);
        MapAnalytics(api);
        MapSettings(api);
        MapAudit(api);
        MapExports(api);
    }

    private static void MapAuth(RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");
        auth.MapPost("/login", async (LoginRequest request, ISender sender, IUtcClock clock, AuthAttemptLimiter limiter, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var ip = ClientIp(httpContext);
            var limit = limiter.Check("login", request.Email, ip, 5, LoginFailureWindow);
            if (!limit.Allowed)
            {
                return RateLimited(limit);
            }

            try
            {
                var result = await sender.Send(new LoginCommand(request.Email, request.Password, request.DeviceName, ip, clock.UtcNow.AddHours(8), clock.UtcNow.AddDays(30)), cancellationToken);
                limiter.Reset("login", request.Email, ip);
                return Results.Ok(ToAuthResponse(result));
            }
            catch (ForbiddenException)
            {
                limiter.RecordFailure("login", request.Email, ip, LoginFailureWindow);
                throw;
            }
        }).AllowAnonymous().Produces<AuthResponse>(StatusCodes.Status200OK);

        auth.MapPost("/refresh", async (RefreshRequest request, ISender sender, IUtcClock clock, CancellationToken cancellationToken) =>
            Results.Ok(ToAuthResponse(await sender.Send(new RefreshTokenCommand(request.RefreshToken, clock.UtcNow.AddDays(30)), cancellationToken)))).AllowAnonymous().Produces<AuthResponse>(StatusCodes.Status200OK);
        auth.MapPost("/logout", async (LogoutRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new LogoutCommand(request.SessionId), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();
        auth.MapPost("/forgot-password", async (ForgotPasswordRequest request, ISender sender, IUtcClock clock, AuthAttemptLimiter limiter, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var ip = ClientIp(httpContext);
            var limit = limiter.Check("forgot-password", request.Email, ip, 3, ForgotPasswordWindow);
            if (!limit.Allowed)
            {
                return RateLimited(limit);
            }

            limiter.RecordFailure("forgot-password", request.Email, ip, ForgotPasswordWindow);
            return Results.Ok(await sender.Send(new ForgotPasswordCommand(request.Email, clock.UtcNow.AddMinutes(30)), cancellationToken));
        }).AllowAnonymous();
        auth.MapPost("/reset-password", async (ResetPasswordRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken))).AllowAnonymous();

        var invitations = api.MapGroup("/invitations").WithTags("Invitations");
        invitations.MapPost("/accept", async (AcceptInvitationRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new AcceptInvitationCommand(request.Token, request.Password, request.DisplayName, request.FirstName, request.LastName, request.Department), cancellationToken))).AllowAnonymous();
        invitations.MapGet("/", async ([AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetInvitationsQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search }, cancellationToken))).RequireAuthorization("ADMIN");
        invitations.MapPost("/", async (InviteRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Created($"/api/v1/invitations", await sender.Send(new InviteStudentCommand(request.Email, request.ExpiresAt), cancellationToken))).RequireAuthorization("ADMIN");
        invitations.MapPost("/{id:guid}/resend", async (Guid id, ResendInvitationRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new ResendInvitationCommand(id, request.ExpiresAt), cancellationToken))).RequireAuthorization("ADMIN");
        invitations.MapPost("/{id:guid}/revoke", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new RevokeInvitationCommand(id), cancellationToken))).RequireAuthorization("ADMIN");

        var sessions = api.MapGroup("/sessions").RequireAuthorization().WithTags("Sessions");
        sessions.MapGet("/", async (Guid? userId, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetActiveSessionsQuery(userId), cancellationToken)));
        sessions.MapDelete("/{sessionId:guid}", async (Guid sessionId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RevokeSessionCommand(sessionId), cancellationToken);
            return Results.NoContent();
        });
        sessions.MapDelete("/", async (Guid? userId, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(new { revoked = await sender.Send(new RevokeAllSessionsCommand(userId), cancellationToken) }));
    }

    private static AuthResponse ToAuthResponse(AuthenticationResultDto result) =>
        new(result.AccessToken, result.RawRefreshToken, result.AccessTokenExpiresAt, result.AccessTokenExpiresAt, result.RefreshTokenExpiresAt, result.SessionId, result.SessionExpiresAt, new AuthUserResponse(result.UserId, result.Email, result.DisplayName, result.Roles));

    private static void MapCatalog(RouteGroupBuilder api)
    {
        api.MapGet("/skills", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetSkillsQuery(), cancellationToken))).RequireAuthorization();
        api.MapPost("/skills", async (CreateSkillCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/skills", await sender.Send(request, cancellationToken))).RequireAuthorization("ADMIN");
        api.MapPost("/students/{studentId:guid}/skills", async (Guid studentId, UpsertStudentSkillRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new UpsertStudentSkillCommand(studentId, request.SkillId, request.Level), cancellationToken))).RequireAuthorization();

        api.MapGet("/categories", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetCategoriesQuery(), cancellationToken))).RequireAuthorization();
        api.MapPost("/categories", async (CreateCategoryCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/categories", await sender.Send(request, cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
    }

    private static void MapStudents(RouteGroupBuilder api)
    {
        var students = api.MapGroup("/students").RequireAuthorization().WithTags("Students");
        students.MapGet("/", async ([AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetStudentsQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search, SortBy = page.SortBy, SortDirection = page.SortDirection }, cancellationToken)));
        students.MapGet("/me", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetCurrentStudentProfileQuery(), cancellationToken)));
        students.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetStudentQuery(id), cancellationToken)));
        students.MapPut("/{id:guid}", async (Guid id, UpdateStudentRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new UpdateStudentProfileCommand(id, request.FirstName, request.LastName, request.Email, request.Department), cancellationToken)));
        students.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new ActivateStudentCommand(id), cancellationToken))).RequireAuthorization("ADMIN");
        students.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new DeactivateStudentCommand(id), cancellationToken))).RequireAuthorization("ADMIN");
    }

    private static void MapTasks(RouteGroupBuilder api)
    {
        var tasks = api.MapGroup("/tasks").RequireAuthorization().WithTags("Tasks");
        tasks.MapGet("/", async ([AsParameters] TaskQueryParams query, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetTasksQuery { Page = query.Page, PageSize = ClampPageSize(query.PageSize), Search = query.Search, SortBy = query.SortBy, SortDirection = query.SortDirection, StudentId = query.StudentId, Status = query.Status, Priority = query.Priority, Difficulty = query.Difficulty, CategoryId = query.CategoryId, DeadlineFrom = query.DeadlineFrom, DeadlineTo = query.DeadlineTo }, cancellationToken)));
        tasks.MapGet("/my", async ([AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetMyTasksQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search, SortBy = page.SortBy, SortDirection = page.SortDirection }, cancellationToken)));
        tasks.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskQuery(id), cancellationToken)));
        tasks.MapPost("/", async (CreateTaskCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/tasks", await sender.Send(request, cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        tasks.MapPut("/{id:guid}", async (Guid id, UpdateTaskRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new UpdateTaskCommand(id, request.Title, request.Description, request.CategoryId, request.SemesterId, request.Priority, request.Difficulty, request.StartDate, request.Deadline, request.EstimatedDurationMinutes, request.ConcurrencyToken), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        tasks.MapPost("/{id:guid}/assign", async (Guid id, AssignTaskRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new AssignTaskCommand(id, request.StudentId, request.Reason), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        tasks.MapPost("/{id:guid}/reassign", async (Guid id, ReassignTaskRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new ReassignTaskCommand(id, request.NewStudentId, request.Reason), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        tasks.MapPost("/{id:guid}/unassign", async (Guid id, ReasonRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UnassignTaskCommand(id, request.Reason), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        tasks.MapPost("/{id:guid}/accept", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new AcceptTaskCommand(id), cancellationToken)));
        tasks.MapPost("/{id:guid}/start", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new StartTaskCommand(id), cancellationToken)));
        tasks.MapPost("/{id:guid}/submit", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new SubmitTaskCommand(id), cancellationToken)));
        tasks.MapPost("/{id:guid}/cancel", async (Guid id, ReasonRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new CancelTaskCommand(id, request.Reason), cancellationToken)));
        tasks.MapGet("/{id:guid}/history", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskAssignmentHistoryQuery(id), cancellationToken)));
        tasks.MapGet("/{id:guid}/dependencies", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskDependenciesQuery(id), cancellationToken)));
        tasks.MapPost("/{id:guid}/dependencies", async (Guid id, AddDependencyRequest request, ISender sender, CancellationToken cancellationToken) => Results.Created($"/api/v1/tasks/{id}/dependencies", await sender.Send(new AddTaskDependencyCommand(id, request.DependsOnTaskId), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        tasks.MapGet("/{id:guid}/skills", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskRequiredSkillsQuery(id), cancellationToken)));
        tasks.MapGet("/{id:guid}/comments", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskCommentsQuery(id), cancellationToken)));
        tasks.MapPost("/{id:guid}/comments", async (Guid id, TaskCommentRequest request, ISender sender, CancellationToken cancellationToken) => Results.Created($"/api/v1/tasks/{id}/comments", await sender.Send(new AddTaskCommentCommand(id, request.Content, request.Visibility), cancellationToken)));
        tasks.MapPut("/{id:guid}/comments/{commentId:guid}", async (Guid id, Guid commentId, UpdateCommentRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UpdateTaskCommentCommand(id, commentId, request.Content), cancellationToken)));
        tasks.MapDelete("/{id:guid}/comments/{commentId:guid}", async (Guid id, Guid commentId, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteTaskCommentCommand(id, commentId), cancellationToken); return Results.NoContent(); });
        tasks.MapGet("/{id:guid}/checklist", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskChecklistQuery(id), cancellationToken)));
        tasks.MapPost("/{id:guid}/checklist", async (Guid id, ChecklistItemRequest request, ISender sender, CancellationToken cancellationToken) => Results.Created($"/api/v1/tasks/{id}/checklist", await sender.Send(new AddChecklistItemCommand(id, request.Title, request.Order), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        tasks.MapPost("/{id:guid}/checklist/{itemId:guid}/complete", async (Guid id, Guid itemId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new CompleteChecklistItemCommand(id, itemId), cancellationToken)));
        tasks.MapPost("/{id:guid}/checklist/{itemId:guid}/uncomplete", async (Guid id, Guid itemId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UncompleteChecklistItemCommand(id, itemId), cancellationToken)));
        tasks.MapGet("/{id:guid}/recommendations", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetAssignmentRecommendationsQuery(id), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
    }

    private static void MapRequests(RouteGroupBuilder api)
    {
        var requests = api.MapGroup("/requests").RequireAuthorization().WithTags("Requests");
        requests.MapGet("/", async ([AsParameters] RequestQueryParams query, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetTaskRequestsQuery { Page = query.Page, PageSize = ClampPageSize(query.PageSize), Search = query.Search, TaskId = query.TaskId, Type = query.Type, Status = query.Status }, cancellationToken)));
        requests.MapPost("/extension", async (CreateExtensionRequestCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/requests", await sender.Send(request, cancellationToken)));
        requests.MapPost("/reassignment", async (CreateReassignmentRequestCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/requests", await sender.Send(request, cancellationToken)));
        requests.MapPost("/{id:guid}/approve", async (Guid id, ApproveRequestBody request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new ApproveTaskRequestCommand(id, request.ReviewerComment, request.NewAssigneeId), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        requests.MapPost("/{id:guid}/reject", async (Guid id, RejectRequestBody request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new RejectTaskRequestCommand(id, request.ReviewerComment), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        requests.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new CancelTaskRequestCommand(id), cancellationToken)));
    }

    private static void MapSubmissions(RouteGroupBuilder api)
    {
        var submissions = api.MapGroup("/submissions").RequireAuthorization().WithTags("Submissions");
        api.MapGet("/tasks/{taskId:guid}/submissions", async (Guid taskId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskSubmissionsQuery(taskId), cancellationToken))).RequireAuthorization();
        api.MapPost("/tasks/{taskId:guid}/submissions/uploads", async (Guid taskId, SubmissionUploadRequest request, ISender sender, CancellationToken cancellationToken) => Results.Created($"/api/v1/tasks/{taskId}/submissions", await sender.Send(new InitiateSubmissionUploadCommand(taskId, request.FileName, request.FileSize, request.MimeType, request.FileExtension, request.ContentHash), cancellationToken))).RequireAuthorization("STUDENT");
        submissions.MapGet("/{id:guid}/versions", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetSubmissionVersionsQuery(id), cancellationToken)));
        submissions.MapPost("/versions/{versionId:guid}/complete", async (Guid versionId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new CompleteSubmissionUploadCommand(versionId), cancellationToken))).RequireAuthorization("STUDENT");
        submissions.MapPost("/{id:guid}/approve", async (Guid id, ReviewSubmissionRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new ApproveSubmissionCommand(id, request.ReviewerComment), cancellationToken))).RequireAuthorization("REVIEWERS");
        submissions.MapPost("/{id:guid}/revision-request", async (Guid id, ReviewSubmissionRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new RequestSubmissionRevisionCommand(id, request.ReviewerComment ?? string.Empty), cancellationToken))).RequireAuthorization("REVIEWERS");
    }

    private static void MapMarketplace(RouteGroupBuilder api)
    {
        var marketplace = api.MapGroup("/marketplace").RequireAuthorization().WithTags("Marketplace");
        marketplace.MapGet("/listings", async ([AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetMarketplaceListingsQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search }, cancellationToken)));
        marketplace.MapPost("/tasks/{taskId:guid}/publish", async (Guid taskId, PublishMarketplaceRequest request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/marketplace/listings", await sender.Send(new PublishTaskToMarketplaceCommand(taskId, request.ApprovalMode, request.ExpiresAt), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        marketplace.MapPost("/listings/{id:guid}/unpublish", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UnpublishTaskCommand(id), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        marketplace.MapPost("/listings/{id:guid}/claims", async (Guid id, ClaimMarketplaceRequest request, ISender sender, CancellationToken cancellationToken) => Results.Created($"/api/v1/marketplace/listings/{id}/claims", await sender.Send(new ClaimMarketplaceTaskCommand(id, request.ExpiresAt), cancellationToken))).RequireAuthorization("STUDENT");
        marketplace.MapPost("/claims/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new ApproveMarketplaceClaimCommand(id), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        marketplace.MapPost("/claims/{id:guid}/reject", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new RejectMarketplaceClaimCommand(id), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        marketplace.MapPost("/claims/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new CancelMarketplaceClaimCommand(id), cancellationToken))).RequireAuthorization("STUDENT");
    }

    private static void MapSchedules(RouteGroupBuilder api)
    {
        var semesters = api.MapGroup("/semesters").RequireAuthorization().WithTags("Semesters");
        semesters.MapGet("/", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetSemestersQuery(), cancellationToken)));
        semesters.MapGet("/active", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetActiveSemesterQuery(), cancellationToken)));
        semesters.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetSemesterQuery(id), cancellationToken)));
        semesters.MapPost("/", async (CreateSemesterCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/semesters", await sender.Send(request, cancellationToken))).RequireAuthorization("ADMIN");
        semesters.MapPut("/{id:guid}", async (Guid id, UpdateSemesterRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UpdateSemesterCommand(id, request.Name, request.StartDate, request.EndDate, request.Status), cancellationToken))).RequireAuthorization("ADMIN");
        semesters.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new ActivateSemesterCommand(id), cancellationToken))).RequireAuthorization("ADMIN");
        semesters.MapPost("/{id:guid}/archive", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new ArchiveSemesterCommand(id), cancellationToken))).RequireAuthorization("ADMIN");
        semesters.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteSemesterCommand(id), cancellationToken); return Results.NoContent(); }).RequireAuthorization("ADMIN");

        var schedules = api.MapGroup("/schedules").RequireAuthorization().WithTags("Schedules");
        schedules.MapGet("/students/{studentId:guid}", async (Guid studentId, Guid? semesterId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetStudentScheduleQuery(studentId, semesterId), cancellationToken)));
        schedules.MapGet("/students/{studentId:guid}/current", async (Guid studentId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetCurrentSemesterScheduleQuery(studentId), cancellationToken)));
        schedules.MapPost("/", async (CreateCourseScheduleCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/schedules", await sender.Send(request, cancellationToken)));
        schedules.MapPut("/{id:guid}", async (Guid id, ScheduleRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UpdateCourseScheduleCommand(id, request.CourseName, request.CourseCode, request.DayOfWeek, request.StartTime, request.EndTime, request.Location), cancellationToken)));
        schedules.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteCourseScheduleCommand(id), cancellationToken); return Results.NoContent(); });

        var availability = api.MapGroup("/availability").RequireAuthorization().WithTags("Availability");
        availability.MapGet("/students/{studentId:guid}", async (Guid studentId, Guid? semesterId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetStudentAvailabilityQuery(studentId, semesterId), cancellationToken)));
        availability.MapGet("/students/{studentId:guid}/current", async (Guid studentId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetCurrentSemesterAvailabilityQuery(studentId), cancellationToken)));
        availability.MapPost("/", async (CreateAvailabilityCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/availability", await sender.Send(request, cancellationToken)));
        availability.MapPut("/{id:guid}", async (Guid id, AvailabilityRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UpdateAvailabilityCommand(id, request.DayOfWeek, request.StartTime, request.EndTime, request.Status, request.Reason), cancellationToken)));
        availability.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteAvailabilityCommand(id), cancellationToken); return Results.NoContent(); });
    }

    private static void MapFiles(RouteGroupBuilder api)
    {
        var files = api.MapGroup("/files").RequireAuthorization().WithTags("Files");
        files.MapGet("/", async ([AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetDepartmentFilesQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search }, cancellationToken)));
        files.MapPost("/uploads", async (DepartmentFileUploadRequest request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/files", await sender.Send(new InitiateDepartmentFileUploadCommand(request.FolderId, request.FileName, request.FileSize, request.MimeType, request.FileExtension, request.ContentHash), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        files.MapPost("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new CompleteDepartmentFileUploadCommand(id), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        files.MapGet("/{id:guid}/download", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetDepartmentFileDownloadQuery(id), cancellationToken)));
        files.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteDepartmentFileCommand(id), cancellationToken); return Results.NoContent(); }).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        files.MapGet("/folders", async (Guid? parentFolderId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetFileFoldersQuery(parentFolderId), cancellationToken)));
        files.MapPost("/folders", async (CreateFileFolderCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/files/folders", await sender.Send(request, cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        files.MapPut("/folders/{id:guid}", async (Guid id, RenameFolderRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new RenameFileFolderCommand(id, request.Name), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT");
        files.MapDelete("/folders/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteFileFolderCommand(id), cancellationToken); return Results.NoContent(); }).RequireAuthorization("STAFF_TASK_MANAGEMENT");
    }

    private static void MapAnnouncements(RouteGroupBuilder api)
    {
        var announcements = api.MapGroup("/announcements").RequireAuthorization().WithTags("Announcements");
        announcements.MapGet("/", async ([AsParameters] PageParams page, bool? publishedOnly, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetAnnouncementsQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search, PublishedOnly = publishedOnly ?? true }, cancellationToken)));
        announcements.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetAnnouncementQuery(id), cancellationToken)));
        announcements.MapPost("/", async (CreateAnnouncementCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/announcements", await sender.Send(request, cancellationToken))).RequireAuthorization("ADMIN");
        announcements.MapPut("/{id:guid}", async (Guid id, AnnouncementRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UpdateAnnouncementCommand(id, request.Title, request.Content, request.ExpiresAt, request.IsPinned), cancellationToken))).RequireAuthorization("ADMIN");
        announcements.MapPost("/{id:guid}/publish", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new PublishAnnouncementCommand(id), cancellationToken))).RequireAuthorization("ADMIN");
        announcements.MapPost("/{id:guid}/unpublish", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UnpublishAnnouncementCommand(id), cancellationToken))).RequireAuthorization("ADMIN");
        announcements.MapPost("/{id:guid}/pin", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new PinAnnouncementCommand(id), cancellationToken))).RequireAuthorization("ADMIN");
        announcements.MapPost("/{id:guid}/unpin", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UnpinAnnouncementCommand(id), cancellationToken))).RequireAuthorization("ADMIN");
        announcements.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteAnnouncementCommand(id), cancellationToken); return Results.NoContent(); }).RequireAuthorization("ADMIN");
    }

    private static void MapNotifications(RouteGroupBuilder api)
    {
        var notifications = api.MapGroup("/notifications").RequireAuthorization().WithTags("Notifications");
        notifications.MapGet("/", async ([AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetNotificationsQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search }, cancellationToken)));
        notifications.MapGet("/unread-count", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(new { count = await sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken) }));
        notifications.MapPost("/{id:guid}/read", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new MarkNotificationReadCommand(id), cancellationToken)));
        notifications.MapPost("/read-all", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(new { count = await sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken) }));
        notifications.MapPut("/preferences", async (UpdateNotificationPreferenceCommand request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(request, cancellationToken)));
    }

    private static void MapFeedback(RouteGroupBuilder api)
    {
        api.MapGet("/tasks/{taskId:guid}/feedback", async (Guid taskId, [AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetFeedbackQuery { TaskId = taskId, Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search }, cancellationToken))).RequireAuthorization().WithTags("Feedback");
        api.MapPost("/tasks/{taskId:guid}/feedback", async (Guid taskId, CreateFeedbackRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Created($"/api/v1/tasks/{taskId:D}/feedback", await sender.Send(new CreateFeedbackCommand(taskId, request.StudentId, request.Rating, request.Comment), cancellationToken))).RequireAuthorization("STAFF_TASK_MANAGEMENT").WithTags("Feedback");
        api.MapGet("/students/{studentId:guid}/feedback", async (Guid studentId, [AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetFeedbackQuery { StudentId = studentId, Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search }, cancellationToken))).RequireAuthorization().WithTags("Feedback");
    }

    private static void MapTemplates(RouteGroupBuilder api)
    {
        var templates = api.MapGroup("/templates").RequireAuthorization("STAFF_TASK_MANAGEMENT").WithTags("Templates");
        templates.MapGet("/", async ([AsParameters] PageParams page, Guid? categoryId, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskTemplatesQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search, CategoryId = categoryId }, cancellationToken)));
        templates.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTaskTemplateQuery(id), cancellationToken)));
        templates.MapPost("/", async (CreateTaskTemplateCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/templates", await sender.Send(request, cancellationToken)));
        templates.MapPost("/{id:guid}/create-task", async (Guid id, CreateTaskFromTemplateRequest request, ISender sender, CancellationToken cancellationToken) =>
            Results.Created("/api/v1/tasks", await sender.Send(new CreateTaskFromTemplateCommand(id, request.StartDate, request.Deadline, request.SemesterId), cancellationToken)));
        templates.MapPut("/{id:guid}", async (Guid id, TemplateRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UpdateTaskTemplateCommand(id, request.Title, request.Description, request.CategoryId, request.DefaultPriority, request.DefaultDifficulty, request.EstimatedDurationMinutes, request.ChecklistTemplateJson, request.RequiredSkillsTemplateJson), cancellationToken)));
        templates.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteTaskTemplateCommand(id), cancellationToken); return Results.NoContent(); });

        var recurring = api.MapGroup("/recurring-tasks").RequireAuthorization("STAFF_TASK_MANAGEMENT").WithTags("Recurring Tasks");
        recurring.MapGet("/", async ([AsParameters] PageParams page, bool? isActive, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetRecurringTasksQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search, IsActive = isActive }, cancellationToken)));
        recurring.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetRecurringTaskQuery(id), cancellationToken)));
        recurring.MapPost("/", async (CreateRecurringTaskCommand request, ISender sender, CancellationToken cancellationToken) => Results.Created("/api/v1/recurring-tasks", await sender.Send(request, cancellationToken)));
        recurring.MapPut("/{id:guid}", async (Guid id, RecurringTaskRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UpdateRecurringTaskCommand(id, request.Frequency, request.TimeZoneId, request.LocalRunTime, request.NextRunAt), cancellationToken)));
        recurring.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new ActivateRecurringTaskCommand(id), cancellationToken)));
        recurring.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new DeactivateRecurringTaskCommand(id), cancellationToken)));
        recurring.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => { await sender.Send(new DeleteRecurringTaskCommand(id), cancellationToken); return Results.NoContent(); });
    }

    private static void MapAnalytics(RouteGroupBuilder api)
    {
        var analytics = api.MapGroup("/analytics").RequireAuthorization("STAFF_TASK_MANAGEMENT").WithTags("Analytics");
        analytics.MapGet("/dashboard", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetDashboardAnalyticsQuery(), cancellationToken)));
        analytics.MapGet("/tasks/status", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTasksByStatusAnalyticsQuery(), cancellationToken)));
        analytics.MapGet("/tasks/category", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetTasksByCategoryAnalyticsQuery(), cancellationToken)));
        analytics.MapGet("/workload", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetWorkloadDistributionQuery(), cancellationToken)));
        analytics.MapGet("/requests", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetRequestAnalyticsQuery(), cancellationToken)));
    }

    private static void MapSettings(RouteGroupBuilder api)
    {
        var settings = api.MapGroup("/settings").RequireAuthorization("ADMIN").WithTags("Settings");
        settings.MapGet("/", async (ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetSettingsQuery(), cancellationToken)));
        settings.MapPut("/{key}", async (string key, UpdateSettingRequest request, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new UpdateSettingCommand(key, request.Value, request.ConcurrencyToken), cancellationToken)));
    }

    private static void MapAudit(RouteGroupBuilder api)
    {
        var audit = api.MapGroup("/audit").RequireAuthorization("ADMIN").WithTags("Audit");
        audit.MapGet("/", async ([AsParameters] AuditQueryParams query, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetAuditLogsQuery { Page = query.Page, PageSize = ClampPageSize(query.PageSize), Search = query.Search, Action = query.Action, EntityType = query.EntityType, UserId = query.UserId, From = query.From, To = query.To }, cancellationToken)));
        audit.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) => Results.Ok(await sender.Send(new GetAuditLogQuery(id), cancellationToken)));
    }

    private static void MapExports(RouteGroupBuilder api)
    {
        var exports = api.MapGroup("/exports").RequireAuthorization().WithTags("Exports");
        exports.MapPost("/", async (RequestExportCommand request, ISender sender, CancellationToken cancellationToken) =>
        {
            var accepted = await sender.Send(request, cancellationToken);
            return Results.Accepted(accepted.StatusUrl.ToString(), accepted);
        });
        exports.MapGet("/", async ([AsParameters] PageParams page, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetExportsQuery { Page = page.Page, PageSize = ClampPageSize(page.PageSize), Search = page.Search, SortBy = page.SortBy, SortDirection = page.SortDirection }, cancellationToken)));
        exports.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetExportQuery(id), cancellationToken)));
        exports.MapGet("/{id:guid}/download", async (Guid id, ISender sender, IFileStorage storage, CancellationToken cancellationToken) =>
        {
            var download = await sender.Send(new GetExportDownloadQuery(id), cancellationToken);
            if (download.DownloadUrl.IsAbsoluteUri)
            {
                return Results.Redirect(download.DownloadUrl.ToString());
            }

            var stream = await storage.OpenReadAsync(download.StorageKey, cancellationToken);
            return Results.File(stream, download.MimeType, download.FileName, enableRangeProcessing: true);
        });
    }

    private static int ClampPageSize(int pageSize) => Math.Clamp(pageSize, 1, MaxPageSize);

    private static string ClientIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static IResult RateLimited(AuthLimitResult limit)
    {
        return Results.Problem(
            title: "Too Many Requests",
            detail: "Too many authentication attempts. Retry later.",
            statusCode: StatusCodes.Status429TooManyRequests,
            extensions: new Dictionary<string, object?> { ["retryAfterSeconds"] = Math.Max(1, (int)Math.Ceiling(limit.RetryAfter.TotalSeconds)) });
    }

    public sealed record PageParams(int Page = 1, int PageSize = 20, string? Search = null, string? SortBy = null, string? SortDirection = null);
    public sealed record TaskQueryParams(int Page = 1, int PageSize = 20, string? Search = null, string? SortBy = null, string? SortDirection = null, Guid? StudentId = null, TaskStatus? Status = null, TaskPriority? Priority = null, TaskDifficulty? Difficulty = null, Guid? CategoryId = null, DateTimeOffset? DeadlineFrom = null, DateTimeOffset? DeadlineTo = null);
    public sealed record RequestQueryParams(int Page = 1, int PageSize = 20, string? Search = null, Guid? TaskId = null, RequestType? Type = null, RequestStatus? Status = null);
    public sealed record AuditQueryParams(int Page = 1, int PageSize = 20, string? Search = null, string? Action = null, string? EntityType = null, Guid? UserId = null, DateTimeOffset? From = null, DateTimeOffset? To = null);
    public sealed record LoginRequest(string Email, string Password, string? DeviceName);
    public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt, Guid SessionId, DateTimeOffset SessionExpiresAt, AuthUserResponse User);
    public sealed record AuthUserResponse(Guid Id, string Email, string DisplayName, IReadOnlyCollection<string> Roles);
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record LogoutRequest(Guid SessionId);
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string Token, string NewPassword);
    public sealed record AcceptInvitationRequest(string Token, string Password, string DisplayName, string? FirstName, string? LastName, string? Department);
    public sealed record InviteRequest(string Email, DateTimeOffset ExpiresAt);
    public sealed record ResendInvitationRequest(DateTimeOffset ExpiresAt);
    public sealed record UpsertStudentSkillRequest(Guid SkillId, SkillLevel Level);
    public sealed record UpdateStudentRequest(string FirstName, string LastName, string Email, string Department);
    public sealed record UpdateTaskRequest(string Title, string? Description, Guid CategoryId, Guid? SemesterId, TaskPriority Priority, TaskDifficulty Difficulty, DateTimeOffset? StartDate, DateTimeOffset Deadline, int EstimatedDurationMinutes, Guid ConcurrencyToken);
    public sealed record AssignTaskRequest(Guid StudentId, string? Reason);
    public sealed record ReassignTaskRequest(Guid NewStudentId, string Reason);
    public sealed record ReasonRequest(string Reason);
    public sealed record AddDependencyRequest(Guid DependsOnTaskId);
    public sealed record TaskCommentRequest(string Content, TaskCommentVisibility Visibility);
    public sealed record UpdateCommentRequest(string Content);
    public sealed record ChecklistItemRequest(string Title, int Order);
    public sealed record ApproveRequestBody(string? ReviewerComment, Guid? NewAssigneeId);
    public sealed record RejectRequestBody(string ReviewerComment);
    public sealed record SubmissionUploadRequest(string FileName, long FileSize, string MimeType, string FileExtension, string? ContentHash);
    public sealed record ReviewSubmissionRequest(string? ReviewerComment);
    public sealed record PublishMarketplaceRequest(MarketplaceApprovalMode ApprovalMode, DateTimeOffset? ExpiresAt);
    public sealed record ClaimMarketplaceRequest(DateTimeOffset? ExpiresAt);
    public sealed record UpdateSemesterRequest(string Name, DateOnly StartDate, DateOnly EndDate, SemesterStatus Status);
    public sealed record ScheduleRequest(string CourseName, string CourseCode, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string? Location);
    public sealed record AvailabilityRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, AvailabilityStatus Status, string? Reason);
    public sealed record DepartmentFileUploadRequest(Guid? FolderId, string FileName, long FileSize, string MimeType, string FileExtension, string? ContentHash);
    public sealed record RenameFolderRequest(string Name);
    public sealed record AnnouncementRequest(string Title, string Content, DateTimeOffset? ExpiresAt, bool IsPinned);
    public sealed record CreateFeedbackRequest(Guid StudentId, int? Rating, string? Comment);
    public sealed record TemplateRequest(string Title, string? Description, Guid CategoryId, TaskPriority DefaultPriority, TaskDifficulty DefaultDifficulty, int EstimatedDurationMinutes, string? ChecklistTemplateJson, string? RequiredSkillsTemplateJson);
    public sealed record CreateTaskFromTemplateRequest(DateTimeOffset? StartDate, DateTimeOffset Deadline, Guid? SemesterId);
    public sealed record RecurringTaskRequest(string Frequency, string TimeZoneId, TimeOnly? LocalRunTime, DateTimeOffset NextRunAt);
    public sealed record UpdateSettingRequest(string Value, Guid ConcurrencyToken);
}
