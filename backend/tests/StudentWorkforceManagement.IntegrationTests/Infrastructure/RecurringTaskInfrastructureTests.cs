using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.RecurringTasks.Services;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs.RecurringTasks;
using StudentWorkforceManagement.Infrastructure.Persistence;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;
using DomainTaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.IntegrationTests.Infrastructure;

public sealed class RecurringTaskInfrastructureTests
{
    [Fact]
    public void Recurring_schedule_calculator_preserves_configured_local_time()
    {
        var calculator = new RecurringScheduleCalculator();
        var recurring = new RecurringTask
        {
            Frequency = "Every Monday",
            TimeZoneId = "Europe/Istanbul",
            LocalRunTime = new TimeOnly(9, 0)
        };
        var sundayNineInIstanbul = new DateTimeOffset(2026, 8, 9, 6, 0, 0, TimeSpan.Zero);

        var next = calculator.CalculateNextRun(recurring, sundayNineInIstanbul);

        Assert.Equal(new DateTimeOffset(2026, 8, 10, 6, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void Recurring_schedule_calculator_clamps_monthly_end_of_month()
    {
        var calculator = new RecurringScheduleCalculator();
        var recurring = new RecurringTask
        {
            Frequency = "Monthly",
            TimeZoneId = "Europe/Istanbul",
            LocalRunTime = new TimeOnly(9, 0)
        };
        var januaryThirtyFirstNineInIstanbul = new DateTimeOffset(2026, 1, 31, 6, 0, 0, TimeSpan.Zero);

        var next = calculator.CalculateNextRun(recurring, januaryThirtyFirstNineInIstanbul);

        Assert.Equal(new DateTimeOffset(2026, 2, 28, 6, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public async System.Threading.Tasks.Task Recurring_task_job_records_single_completed_occurrence_and_prevents_duplicate_generation()
    {
        await using var context = CreateContext();
        var dueAt = new DateTimeOffset(2026, 8, 10, 6, 0, 0, TimeSpan.Zero);
        SeedRecurringTask(context, dueAt, "Daily");
        await context.SaveChangesAsync();
        var generator = new FakeRecurringTaskGenerationService(context);
        var job = new RecurringTaskJob(context, generator, new RecurringScheduleCalculator(), new FakeClock(dueAt.AddMinutes(1)), Options.Create(new BackgroundJobOptions { BatchSize = 10, JobRetryAttempts = 3 }), NullLogger<RecurringTaskJob>.Instance);

        var first = await job.RunAsync();
        var second = await job.RunAsync();

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Single(context.RecurringTaskOccurrences);
        Assert.Single(context.Tasks);
        Assert.Equal(RecurringTaskOccurrenceStatus.COMPLETED, context.RecurringTaskOccurrences.Single().Status);
        Assert.Equal(dueAt.AddDays(1), context.RecurringTasks.Single().NextRunAt);
        Assert.Equal(1, generator.Calls);
    }

    [Fact]
    public async System.Threading.Tasks.Task Recurring_task_job_records_unsupported_frequency_as_failed_without_generating_task()
    {
        await using var context = CreateContext();
        var dueAt = new DateTimeOffset(2026, 8, 10, 6, 0, 0, TimeSpan.Zero);
        SeedRecurringTask(context, dueAt, "Quarterly");
        await context.SaveChangesAsync();
        var generator = new FakeRecurringTaskGenerationService(context);
        var job = new RecurringTaskJob(context, generator, new RecurringScheduleCalculator(), new FakeClock(dueAt.AddMinutes(1)), Options.Create(new BackgroundJobOptions { BatchSize = 10, JobRetryAttempts = 3 }), NullLogger<RecurringTaskJob>.Instance);

        var result = await job.RunAsync();

        var occurrence = context.RecurringTaskOccurrences.Single();
        Assert.Equal(0, result);
        Assert.Equal(RecurringTaskOccurrenceStatus.FAILED, occurrence.Status);
        Assert.Equal(1, occurrence.Attempts);
        Assert.Contains("Unsupported recurring frequency", occurrence.FailureReason, StringComparison.Ordinal);
        Assert.Empty(context.Tasks);
        Assert.Equal(0, generator.Calls);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedRecurringTask(ApplicationDbContext context, DateTimeOffset dueAt, string frequency)
    {
        var creator = new User { Id = Guid.NewGuid(), Email = "creator@example.com", DisplayName = "Creator", IsActive = true };
        var category = new Category { Id = Guid.NewGuid(), Name = "Ops" };
        var template = new TaskTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Website Check",
            CategoryId = category.Id,
            DefaultPriority = TaskPriority.MEDIUM,
            DefaultDifficulty = TaskDifficulty.EASY,
            EstimatedDurationMinutes = 30,
            CreatedById = creator.Id
        };
        context.Users.Add(creator);
        context.Categories.Add(category);
        context.TaskTemplates.Add(template);
        context.RecurringTasks.Add(new RecurringTask
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            Frequency = frequency,
            TimeZoneId = "Europe/Istanbul",
            LocalRunTime = new TimeOnly(9, 0),
            NextRunAt = dueAt,
            CreatedById = creator.Id,
            IsActive = true
        });
    }

    private sealed class FakeClock(DateTimeOffset now) : IUtcClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class FakeRecurringTaskGenerationService(ApplicationDbContext context) : IRecurringTaskGenerationService
    {
        public int Calls { get; private set; }

        public async System.Threading.Tasks.Task<TaskDto> GenerateAsync(RecurringTask recurringTask, DateTimeOffset scheduledRunAt, CancellationToken cancellationToken = default)
        {
            Calls += 1;
            var template = await context.TaskTemplates.SingleAsync(template => template.Id == recurringTask.TemplateId, cancellationToken);
            var task = new DomainTask
            {
                Id = Guid.NewGuid(),
                Title = template.Title,
                CategoryId = template.CategoryId,
                Priority = template.DefaultPriority,
                Difficulty = template.DefaultDifficulty,
                Status = DomainTaskStatus.ASSIGNED,
                CreatedById = recurringTask.CreatedById,
                StartDate = scheduledRunAt,
                Deadline = scheduledRunAt.AddDays(7),
                EstimatedDurationMinutes = template.EstimatedDurationMinutes
            };
            context.Tasks.Add(task);
            await context.SaveChangesAsync(cancellationToken);
            return new TaskDto(task.Id, task.Title, task.Description, task.CategoryId, task.SemesterId, task.Priority, task.Difficulty, task.Status, task.CreatedById, task.AssignedStudentId, task.StartDate, task.Deadline, task.EstimatedDurationMinutes, task.CreatedAt, task.UpdatedAt, task.CompletedAt, task.ConcurrencyToken);
        }
    }
}
