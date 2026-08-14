using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Events;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Collaboration.Commands;
using StudentWorkforceManagement.Application.Collaboration.DTOs;
using StudentWorkforceManagement.Application.Collaboration.Queries;
using StudentWorkforceManagement.Application.Marketplace.Queries.GetMarketplaceListings;
using StudentWorkforceManagement.Application.Requests.Commands.CreateTaskRequest;
using StudentWorkforceManagement.Application.Skills.Commands;
using StudentWorkforceManagement.Application.Skills.Queries.GetStudentSkills;
using StudentWorkforceManagement.Application.Students.Commands;
using StudentWorkforceManagement.Application.Submissions.Commands.ReviewSubmission;
using StudentWorkforceManagement.Application.Tasks.Commands.AssignTask;
using StudentWorkforceManagement.Application.Tasks.Commands.CreateTask;
using StudentWorkforceManagement.Application.Tasks.Commands.AddTaskDependency;
using StudentWorkforceManagement.Application.Tasks.Commands.Checklist;
using StudentWorkforceManagement.Application.Tasks.Commands.ReassignTask;
using StudentWorkforceManagement.Application.Tasks.Commands.RequiredSkills;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Application.Tasks.Queries.GetTasks;
using StudentWorkforceManagement.Application.Tasks.Services;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Persistence;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.IntegrationTests.Application;

public sealed class ApplicationWorkflowTests
{
    [Fact]
    public async System.Threading.Tasks.Task Reassign_preserves_previous_assignment_history()
    {
        await using var context = CreateContext();
        var task = SeedTask(context);
        var oldStudent = SeedStudent(context);
        var newStudent = SeedStudent(context);
        var actorId = Guid.NewGuid();
        context.TaskAssignmentHistory.Add(new TaskAssignmentHistory { Id = Guid.NewGuid(), TaskId = task.Id, StudentId = oldStudent.Id, AssignedByUserId = actorId, AssignedAt = DateTimeOffset.UtcNow, Status = AssignmentStatus.ACTIVE, Mode = AssignmentMode.MANUAL, IsActive = true });
        task.AssignedStudentId = oldStudent.Id;
        await context.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(actorId, null, UserRole.TASK_MANAGER);
        var handler = new ReassignTaskCommandHandler(context, currentUser, new AuditService(context, currentUser), new NoOpEventQueue(), new FakeClock());

        await handler.Handle(new ReassignTaskCommand(task.Id, newStudent.Id, "Coverage balance"), CancellationToken.None);
        await context.SaveChangesAsync();

        var history = await context.TaskAssignmentHistory.IgnoreQueryFilters().Where(item => item.TaskId == task.Id).OrderBy(item => item.AssignedAt).ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.False(history[0].IsActive);
        Assert.Equal(AssignmentStatus.REASSIGNED, history[0].Status);
        Assert.True(history[1].IsActive);
        Assert.Equal(newStudent.Id, task.AssignedStudentId);
    }

    [Fact]
    public async System.Threading.Tasks.Task Workload_uses_estimated_minutes_and_excludes_completed_or_cancelled_tasks()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context);
        SeedTask(context, assignedStudentId: student.Id, status: TaskStatus.ASSIGNED, minutes: 120);
        SeedTask(context, assignedStudentId: student.Id, status: TaskStatus.IN_PROGRESS, minutes: 90);
        SeedTask(context, assignedStudentId: student.Id, status: TaskStatus.COMPLETED, minutes: 500);
        SeedTask(context, assignedStudentId: student.Id, status: TaskStatus.CANCELLED, minutes: 300);
        await context.SaveChangesAsync();

        var service = new TaskWorkloadService(context);

        Assert.Equal(210, await service.GetActiveWorkloadMinutesAsync(student.Id));
    }

    [Fact]
    public async System.Threading.Tasks.Task Dependency_service_detects_cycles()
    {
        await using var context = CreateContext();
        var first = SeedTask(context);
        var second = SeedTask(context);
        var third = SeedTask(context);
        context.TaskDependencies.Add(new TaskDependency { Id = Guid.NewGuid(), TaskId = second.Id, DependsOnTaskId = third.Id });
        context.TaskDependencies.Add(new TaskDependency { Id = Guid.NewGuid(), TaskId = third.Id, DependsOnTaskId = first.Id });
        await context.SaveChangesAsync();

        var service = new TaskDependencyService(context);

        Assert.True(await service.WouldCreateCycleAsync(first.Id, second.Id));
    }

    [Fact]
    public async System.Threading.Tasks.Task Add_dependency_command_rejects_circular_dependency()
    {
        await using var context = CreateContext();
        var first = SeedTask(context);
        var second = SeedTask(context);
        context.TaskDependencies.Add(new TaskDependency { Id = Guid.NewGuid(), TaskId = second.Id, DependsOnTaskId = first.Id });
        await context.SaveChangesAsync();
        var handler = new AddTaskDependencyCommandHandler(context, new TaskDependencyService(context));

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new AddTaskDependencyCommand(first.Id, second.Id), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Extension_request_requires_later_deadline_and_own_task()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context);
        var task = SeedTask(context, assignedStudentId: student.Id, status: TaskStatus.IN_PROGRESS);
        await context.SaveChangesAsync();
        var handler = new CreateTaskRequestCommandHandler(context, new FakeCurrentUser(Guid.NewGuid(), student.Id, UserRole.STUDENT));

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new CreateExtensionRequestCommand(task.Id, task.Deadline.AddHours(-1), "Need time"), CancellationToken.None));

        var created = await handler.Handle(new CreateExtensionRequestCommand(task.Id, task.Deadline.AddDays(1), "Need time"), CancellationToken.None);
        Assert.Equal(RequestType.EXTENSION, created.Type);
        Assert.Equal(RequestStatus.PENDING, created.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task Submission_review_blocks_self_review()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context);
        var task = SeedTask(context, assignedStudentId: student.Id, status: TaskStatus.SUBMITTED_FOR_REVIEW);
        var submission = new TaskSubmission { Id = Guid.NewGuid(), TaskId = task.Id, SubmittedById = student.Id, Status = SubmissionStatus.SUBMITTED_FOR_REVIEW, SubmittedAt = DateTimeOffset.UtcNow };
        context.TaskSubmissions.Add(submission);
        await context.SaveChangesAsync();
        var currentUser = new FakeCurrentUser(Guid.NewGuid(), student.Id, UserRole.REVIEWER);
        var handler = new ReviewSubmissionCommandHandler(context, currentUser, new TaskStateMachine(), new AuditService(context, currentUser), new NoOpEventQueue(), new FakeClock());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new ApproveSubmissionCommand(submission.Id), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Task_query_filters_sorts_and_paginates_server_side()
    {
        await using var context = CreateContext();
        var category = SeedCategory(context);
        SeedTask(context, title: "Website Update", categoryId: category.Id, status: TaskStatus.ASSIGNED, minutes: 90);
        SeedTask(context, title: "Archive Forms", categoryId: category.Id, status: TaskStatus.COMPLETED, minutes: 30);
        SeedTask(context, title: "Website Copy", categoryId: category.Id, status: TaskStatus.IN_PROGRESS, minutes: 60);
        await context.SaveChangesAsync();
        var handler = new GetTasksQueryHandler(context, new FakeCurrentUser(Guid.NewGuid(), null, UserRole.ADMIN));

        var page = await handler.Handle(new GetTasksQuery { Search = "website", Page = 1, PageSize = 1, SortBy = "workload", SortDirection = "desc" }, CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasNextPage);
        Assert.Single(page.Items);
        Assert.Equal(90, page.Items.Single().EstimatedDurationMinutes);
    }

    [Fact]
    public async System.Threading.Tasks.Task Task_query_filters_unassigned_server_side()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context);
        SeedTask(context, title: "Assigned Work", assignedStudentId: student.Id);
        SeedTask(context, title: "Unassigned Work", assignedStudentId: null);
        await context.SaveChangesAsync();
        var handler = new GetTasksQueryHandler(context, new FakeCurrentUser(Guid.NewGuid(), null, UserRole.TASK_MANAGER));

        var page = await handler.Handle(new GetTasksQuery { IsAssigned = false, Page = 1, PageSize = 10, Search = "work" }, CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal("Unassigned Work", page.Items.Single().Title);
        Assert.Null(page.Items.Single().AssignedStudentId);
    }

    [Fact]
    public async System.Threading.Tasks.Task Marketplace_listing_query_contains_safe_task_summary_without_detail_fetches()
    {
        await using var context = CreateContext();
        var category = SeedCategory(context);
        var skill = new Skill { Id = Guid.NewGuid(), Name = "Data QA" };
        var task = SeedTask(context, "Marketplace Task", category.Id);
        task.Description = "Public task summary";
        task.Category = category;
        context.Skills.Add(skill);
        context.TaskRequiredSkills.Add(new TaskRequiredSkill { Id = Guid.NewGuid(), TaskId = task.Id, Task = task, SkillId = skill.Id, Skill = skill, MinimumLevel = SkillLevel.INTERMEDIATE });
        context.MarketplaceListings.Add(new MarketplaceListing { Id = Guid.NewGuid(), TaskId = task.Id, Task = task, Status = MarketplaceListingStatus.PUBLISHED, ApprovalMode = MarketplaceApprovalMode.MANUAL_APPROVAL, PublishedAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();
        var handler = new GetMarketplaceListingsQueryHandler(context);

        var page = await handler.Handle(new GetMarketplaceListingsQuery { Status = MarketplaceListingStatus.PUBLISHED, Page = 1, PageSize = 10, Search = "marketplace" }, CancellationToken.None);

        var listing = Assert.Single(page.Items);
        Assert.Equal("Marketplace Task", listing.TaskSummary?.Title);
        Assert.Equal("Public task summary", listing.TaskSummary?.Description);
        Assert.Equal("Data QA", Assert.Single(listing.TaskSummary!.RequiredSkills).SkillName);
    }

    [Fact]
    public async System.Threading.Tasks.Task Required_skill_mutations_add_update_delete_and_reject_duplicates()
    {
        await using var context = CreateContext();
        var task = SeedTask(context);
        var skill = new Skill { Id = Guid.NewGuid(), Name = "Research" };
        context.Skills.Add(skill);
        await context.SaveChangesAsync();
        var handler = new TaskRequiredSkillCommandHandler(context);

        var added = await handler.Handle(new AddTaskRequiredSkillCommand(task.Id, skill.Id, SkillLevel.BEGINNER), CancellationToken.None);
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new AddTaskRequiredSkillCommand(task.Id, skill.Id, SkillLevel.BEGINNER), CancellationToken.None));
        var updated = await handler.Handle(new UpdateTaskRequiredSkillCommand(task.Id, skill.Id, SkillLevel.ADVANCED), CancellationToken.None);
        await handler.Handle(new DeleteTaskRequiredSkillCommand(task.Id, skill.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(SkillLevel.BEGINNER, added.MinimumLevel);
        Assert.Equal(SkillLevel.ADVANCED, updated.MinimumLevel);
        Assert.Empty(context.TaskRequiredSkills);
    }

    [Fact]
    public async System.Threading.Tasks.Task Student_skills_query_returns_names_levels_empty_and_missing_student_semantics()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context);
        var otherStudent = SeedStudent(context);
        var first = new Skill { Id = Guid.NewGuid(), Name = "Research" };
        var second = new Skill { Id = Guid.NewGuid(), Name = "Data QA" };
        context.Skills.AddRange(first, second);
        context.StudentSkills.AddRange(
            new StudentSkill { Id = Guid.NewGuid(), StudentId = student.Id, SkillId = first.Id, Skill = first, Level = SkillLevel.ADVANCED },
            new StudentSkill { Id = Guid.NewGuid(), StudentId = student.Id, SkillId = second.Id, Skill = second, Level = SkillLevel.INTERMEDIATE });
        await context.SaveChangesAsync();

        var handler = new GetStudentSkillsQueryHandler(context, new FakeCurrentUser(Guid.NewGuid(), null, UserRole.ADMIN));
        var skills = await handler.Handle(new GetStudentSkillsQuery(student.Id), CancellationToken.None);
        var empty = await handler.Handle(new GetStudentSkillsQuery(otherStudent.Id), CancellationToken.None);

        Assert.Equal(new[] { "Data QA", "Research" }, skills.Select(skill => skill.Name).ToArray());
        Assert.Equal(SkillLevel.INTERMEDIATE, skills.First().Level);
        Assert.Equal(second.Id, skills.First().SkillId);
        Assert.Empty(empty);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetStudentSkillsQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Student_skills_query_blocks_cross_student_reads_and_reflects_upsert_updates()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context);
        var otherStudent = SeedStudent(context);
        var skill = new Skill { Id = Guid.NewGuid(), Name = "Data QA" };
        context.Skills.Add(skill);
        await context.SaveChangesAsync();

        var studentUser = new FakeCurrentUser(Guid.NewGuid(), student.Id, UserRole.STUDENT);
        var readHandler = new GetStudentSkillsQueryHandler(context, studentUser);
        var writeHandler = new SkillCommandHandler(context, studentUser);

        var created = await writeHandler.Handle(new UpsertStudentSkillCommand(student.Id, skill.Id, SkillLevel.BEGINNER), CancellationToken.None);
        var firstRead = await readHandler.Handle(new GetStudentSkillsQuery(student.Id), CancellationToken.None);
        var updated = await writeHandler.Handle(new UpsertStudentSkillCommand(student.Id, skill.Id, SkillLevel.EXPERT), CancellationToken.None);
        var secondRead = await readHandler.Handle(new GetStudentSkillsQuery(student.Id), CancellationToken.None);

        Assert.Equal(SkillLevel.BEGINNER, created.Level);
        Assert.Equal(SkillLevel.BEGINNER, Assert.Single(firstRead).Level);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(SkillLevel.EXPERT, Assert.Single(secondRead).Level);
        await Assert.ThrowsAsync<ForbiddenException>(() => readHandler.Handle(new GetStudentSkillsQuery(otherStudent.Id), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Checklist_update_delete_and_reorder_are_task_scoped_and_deterministic()
    {
        await using var context = CreateContext();
        var task = SeedTask(context);
        var otherTask = SeedTask(context);
        await context.SaveChangesAsync();
        var handler = new TaskChecklistCommandHandler(context, new FakeCurrentUser(Guid.NewGuid(), null, UserRole.TASK_MANAGER), new FakeClock());
        var first = await handler.Handle(new AddChecklistItemCommand(task.Id, "First", 0), CancellationToken.None);
        var second = await handler.Handle(new AddChecklistItemCommand(task.Id, "Second", 1), CancellationToken.None);
        var foreign = await handler.Handle(new AddChecklistItemCommand(otherTask.Id, "Foreign", 0), CancellationToken.None);

        var updated = await handler.Handle(new UpdateChecklistItemCommand(task.Id, first.Id, "Updated first"), CancellationToken.None);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdateChecklistItemCommand(task.Id, foreign.Id, "Nope"), CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new ReorderChecklistCommand(task.Id, new[] { new ReorderChecklistItem(second.Id, 0), new ReorderChecklistItem(foreign.Id, 1) }), CancellationToken.None));
        var reordered = await handler.Handle(new ReorderChecklistCommand(task.Id, new[] { new ReorderChecklistItem(second.Id, 0), new ReorderChecklistItem(first.Id, 1) }), CancellationToken.None);
        await handler.Handle(new DeleteChecklistItemCommand(task.Id, first.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal("Updated first", updated.Title);
        Assert.Equal(new[] { second.Id, first.Id }, reordered.Select(item => item.Id).ToArray());
        Assert.Single(context.TaskChecklistItems.Where(item => item.TaskId == task.Id));
    }

    [Fact]
    public async System.Threading.Tasks.Task Collaborative_assignment_preserves_multiple_active_students_and_planned_effort()
    {
        await using var context = CreateContext();
        var task = SeedTask(context, assignedStudentId: null);
        var first = SeedStudent(context);
        var second = SeedStudent(context);
        var actorId = Guid.NewGuid();
        await context.SaveChangesAsync();
        var currentUser = new FakeCurrentUser(actorId, null, UserRole.TASK_MANAGER);
        var handler = new AssignTaskCommandHandler(context, currentUser, new AuditService(context, currentUser), new NoOpEventQueue(), new FakeClock());

        await handler.Handle(new AssignTaskCommand(task.Id, first.Id, "primary", 90), CancellationToken.None);
        await handler.Handle(new AssignTaskCommand(task.Id, second.Id, "pair work", 45), CancellationToken.None);
        await context.SaveChangesAsync();

        var active = await context.TaskAssignmentHistory.Where(item => item.TaskId == task.Id && item.IsActive).OrderBy(item => item.AssignedAt).ToListAsync();
        Assert.Equal(2, active.Count);
        Assert.Equal(first.Id, task.AssignedStudentId);
        Assert.Contains(active, item => item.StudentId == first.Id && item.PlannedEffortMinutes == 90);
        Assert.Contains(active, item => item.StudentId == second.Id && item.PlannedEffortMinutes == 45);
    }

    [Fact]
    public async System.Threading.Tasks.Task Nudge_requires_co_assignees_and_enforces_cooldown()
    {
        await using var context = CreateContext();
        var sender = SeedStudent(context);
        var recipient = SeedStudent(context);
        var task = SeedTask(context, assignedStudentId: sender.Id);
        var now = new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero);
        context.TaskAssignmentHistory.AddRange(
            new TaskAssignmentHistory { Id = Guid.NewGuid(), TaskId = task.Id, StudentId = sender.Id, AssignedByUserId = Guid.NewGuid(), AssignedAt = now, Status = AssignmentStatus.ACTIVE, Mode = AssignmentMode.MANUAL, IsActive = true },
            new TaskAssignmentHistory { Id = Guid.NewGuid(), TaskId = task.Id, StudentId = recipient.Id, AssignedByUserId = Guid.NewGuid(), AssignedAt = now, Status = AssignmentStatus.ACTIVE, Mode = AssignmentMode.MANUAL, IsActive = true });
        await context.SaveChangesAsync();
        var currentUser = new FakeCurrentUser(sender.UserId, sender.Id, UserRole.STUDENT);
        var commandHandler = CreateCollaborationCommandHandler(context, currentUser, new FakeClock(now));
        var queryHandler = new CollaborationQueryHandler(context, currentUser, new FakeClock(now.AddMinutes(10)));

        var sent = await commandHandler.Handle(new SendTaskNudgeCommand(task.Id, recipient.Id), CancellationToken.None);
        var eligibility = await queryHandler.Handle(new GetTaskNudgeEligibilityQuery(task.Id, recipient.Id), CancellationToken.None);

        Assert.Equal(now.AddHours(3), sent.NextAllowedAt);
        Assert.False(eligibility.CanSend);
        Assert.Equal(now.AddHours(3), eligibility.NextAllowedAt);
        await Assert.ThrowsAsync<ConflictException>(() => commandHandler.Handle(new SendTaskNudgeCommand(task.Id, recipient.Id), CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Timesheet_entry_submit_review_and_query_use_persisted_values()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context);
        student.WeeklyTargetMinutes = 480;
        var task = SeedTask(context, assignedStudentId: student.Id);
        context.TaskAssignmentHistory.Add(new TaskAssignmentHistory { Id = Guid.NewGuid(), TaskId = task.Id, StudentId = student.Id, AssignedByUserId = Guid.NewGuid(), AssignedAt = DateTimeOffset.UtcNow, Status = AssignmentStatus.ACTIVE, Mode = AssignmentMode.MANUAL, IsActive = true });
        await context.SaveChangesAsync();
        var studentUser = new FakeCurrentUser(student.UserId, student.Id, UserRole.STUDENT);
        var commandHandler = CreateCollaborationCommandHandler(context, studentUser);

        var week = await commandHandler.Handle(new UpsertTimesheetEntryCommand(null, task.Id, new DateOnly(2026, 8, 10), 75, "handoff"), CancellationToken.None);
        week = await commandHandler.Handle(new SubmitTimesheetWeekCommand(week.Id), CancellationToken.None);
        var reviewHandler = CreateCollaborationCommandHandler(context, new FakeCurrentUser(Guid.NewGuid(), null, UserRole.TASK_MANAGER));
        var reviewed = await reviewHandler.Handle(new ReviewTimesheetWeekCommand(week.Id, TimesheetStatus.APPROVED, null), CancellationToken.None);
        var query = await new CollaborationQueryHandler(context, studentUser, new FakeClock()).Handle(new GetTimesheetWeeksQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(TimesheetStatus.APPROVED, reviewed.Status);
        Assert.Equal(480, reviewed.TargetMinutes);
        Assert.Equal(75, reviewed.TotalMinutes);
        Assert.Equal("handoff", Assert.Single(query.Items).Entries.Single().Note);
    }

    [Fact]
    public async System.Threading.Tasks.Task Admin_can_store_independent_weekly_targets_per_student()
    {
        await using var context = CreateContext();
        var first = SeedStudent(context);
        var second = SeedStudent(context);
        var third = SeedStudent(context);
        await context.SaveChangesAsync();
        var handler = new StudentCommandHandler(context, new FakeCurrentUser(Guid.NewGuid(), null, UserRole.ADMIN));

        await handler.Handle(new UpdateStudentProfileCommand(first.Id, first.FirstName, first.LastName, first.Email, first.Department, 600), CancellationToken.None);
        await handler.Handle(new UpdateStudentProfileCommand(second.Id, second.FirstName, second.LastName, second.Email, second.Department, 480), CancellationToken.None);
        await handler.Handle(new UpdateStudentProfileCommand(third.Id, third.FirstName, third.LastName, third.Email, third.Department, 720), CancellationToken.None);

        Assert.Equal(600, await context.Students.Where(student => student.Id == first.Id).Select(student => student.WeeklyTargetMinutes).SingleAsync());
        Assert.Equal(480, await context.Students.Where(student => student.Id == second.Id).Select(student => student.WeeklyTargetMinutes).SingleAsync());
        Assert.Equal(720, await context.Students.Where(student => student.Id == third.Id).Select(student => student.WeeklyTargetMinutes).SingleAsync());
    }

    [Fact]
    public async System.Threading.Tasks.Task Timesheet_week_snapshots_student_target_and_keeps_history_when_student_changes()
    {
        await using var context = CreateContext();
        var student = SeedStudent(context);
        student.WeeklyTargetMinutes = 600;
        var task = SeedTask(context, assignedStudentId: student.Id);
        context.TaskAssignmentHistory.Add(new TaskAssignmentHistory { Id = Guid.NewGuid(), TaskId = task.Id, StudentId = student.Id, AssignedByUserId = Guid.NewGuid(), AssignedAt = DateTimeOffset.UtcNow, Status = AssignmentStatus.ACTIVE, Mode = AssignmentMode.MANUAL, IsActive = true });
        await context.SaveChangesAsync();
        var handler = CreateCollaborationCommandHandler(context, new FakeCurrentUser(student.UserId, student.Id, UserRole.STUDENT));

        var week = await handler.Handle(new UpsertTimesheetEntryCommand(null, task.Id, new DateOnly(2026, 8, 10), 30, null), CancellationToken.None);
        student.WeeklyTargetMinutes = 720;
        await context.SaveChangesAsync();
        var queried = await new CollaborationQueryHandler(context, new FakeCurrentUser(student.UserId, student.Id, UserRole.STUDENT), new FakeClock(new DateTimeOffset(2026, 8, 12, 9, 0, 0, TimeSpan.Zero))).Handle(new GetTimesheetWeeksQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(600, week.TargetMinutes);
        Assert.Equal(600, Assert.Single(queried.Items).TargetMinutes);
    }

    [Fact]
    public async System.Threading.Tasks.Task Recommendations_use_each_student_target_and_mark_missing_target_as_not_configured()
    {
        await using var context = CreateContext();
        var configured = SeedStudent(context);
        configured.FirstName = "Configured";
        configured.WeeklyTargetMinutes = 480;
        var missing = SeedStudent(context);
        missing.FirstName = "Missing";
        missing.WeeklyTargetMinutes = null;
        var task = SeedTask(context, minutes: 120);
        await context.SaveChangesAsync();
        var service = new AssignmentRecommendationService(context, new TaskWorkloadService(context), new SkillMatchingService());

        var recommendations = await service.GetRecommendationsAsync(task.Id, CancellationToken.None);

        var configuredResult = recommendations.Single(item => item.StudentId == configured.Id);
        var missingResult = recommendations.Single(item => item.StudentId == missing.Id);
        Assert.Equal(480, configuredResult.WeeklyTargetMinutes);
        Assert.Null(missingResult.WeeklyTargetMinutes);
        Assert.Contains("target-not-configured", missingResult.Reasons);
        Assert.DoesNotContain(missingResult.Reasons, reason => reason.Contains("/ 0 min", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async System.Threading.Tasks.Task Meeting_slot_recommendations_prefer_overlapping_available_ranges()
    {
        await using var context = CreateContext();
        var first = SeedStudent(context);
        var second = SeedStudent(context);
        await context.SaveChangesAsync();
        var staff = new FakeCurrentUser(Guid.NewGuid(), null, UserRole.TASK_MANAGER);
        var commandHandler = CreateCollaborationCommandHandler(context, staff);
        var meeting = await commandHandler.Handle(new CreateMeetingCommand("Planning", MeetingType.IN_PERSON, DateTimeOffset.UtcNow.AddDays(1), new[] { first.Id, second.Id }, "Campus", null), CancellationToken.None);

        await CreateCollaborationCommandHandler(context, new FakeCurrentUser(first.UserId, first.Id, UserRole.STUDENT)).Handle(new RespondToMeetingCommand(meeting.Id, CampusPresence.ON_CAMPUS, """[{"startAt":"2026-08-10T10:00:00Z","endAt":"2026-08-10T12:00:00Z"}]""", null), CancellationToken.None);
        await CreateCollaborationCommandHandler(context, new FakeCurrentUser(second.UserId, second.Id, UserRole.STUDENT)).Handle(new RespondToMeetingCommand(meeting.Id, CampusPresence.OFF_CAMPUS, """[{"startAt":"2026-08-10T10:00:00Z","endAt":"2026-08-10T11:00:00Z"}]""", null), CancellationToken.None);
        var slots = await new CollaborationQueryHandler(context, staff, new FakeClock()).Handle(new GetMeetingSlotRecommendationsQuery(meeting.Id), CancellationToken.None);

        var best = Assert.Single(slots);
        Assert.Equal(2, best.AvailableCount);
        Assert.Equal(2, best.ParticipantCount);
        Assert.Equal(new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero), best.StartAt);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Category SeedCategory(ApplicationDbContext context)
    {
        var category = new Category { Id = Guid.NewGuid(), Name = $"Category-{Guid.NewGuid():N}" };
        context.Categories.Add(category);
        return category;
    }

    private static Student SeedStudent(ApplicationDbContext context)
    {
        var student = new Student { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), FirstName = "Ada", LastName = "Lovelace", Email = $"ada-{Guid.NewGuid():N}@example.com", Department = "Operations", IsActive = true };
        context.Students.Add(student);
        return student;
    }

    private static DomainTask SeedTask(ApplicationDbContext context, string title = "Task", Guid? categoryId = null, Guid? assignedStudentId = null, TaskStatus status = TaskStatus.ASSIGNED, int minutes = 60)
    {
        var category = categoryId.HasValue ? null : SeedCategory(context);
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            Title = title,
            CategoryId = categoryId ?? category!.Id,
            Priority = TaskPriority.MEDIUM,
            Difficulty = TaskDifficulty.EASY,
            Status = status,
            CreatedById = Guid.NewGuid(),
            AssignedStudentId = assignedStudentId,
            Deadline = DateTimeOffset.UtcNow.AddDays(7),
            EstimatedDurationMinutes = minutes
        };
        context.Tasks.Add(task);
        return task;
    }

    private sealed class FakeCurrentUser(Guid userId, Guid? studentId, params UserRole[] roles) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
        public Guid? StudentId { get; } = studentId;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<UserRole> Roles { get; } = roles;
    }

    private static CollaborationCommandHandler CreateCollaborationCommandHandler(ApplicationDbContext context, ICurrentUserService currentUser, IUtcClock? clock = null) =>
        new(context, currentUser, new CapturingNotificationIntentService(), clock ?? new FakeClock(), new ThrowingCreateTaskHandler());

    private sealed class FakeClock(DateTimeOffset? utcNow = null) : IUtcClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow ?? DateTimeOffset.UtcNow;
    }

    private sealed class NoOpEventQueue : IApplicationEventQueue
    {
        public void Enqueue(INotification notification) { }
        public System.Threading.Tasks.Task PublishQueuedAsync(CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
    }

    private sealed class CapturingNotificationIntentService : INotificationIntentService
    {
        public System.Threading.Tasks.Task CreateAsync(Guid userId, NotificationType type, string title, string message, string? relatedEntityType, Guid? relatedEntityId, string? idempotencyKey = null, CancellationToken cancellationToken = default) =>
            System.Threading.Tasks.Task.CompletedTask;
    }

    private sealed class ThrowingCreateTaskHandler : IRequestHandler<CreateTaskCommand, TaskDto>
    {
        public System.Threading.Tasks.Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Action item conversion is covered by API integration tests.");
    }
}
