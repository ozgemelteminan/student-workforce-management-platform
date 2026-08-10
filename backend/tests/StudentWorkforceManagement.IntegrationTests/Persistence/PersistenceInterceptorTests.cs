using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Infrastructure.Persistence.Interceptors;
using ThreadingTask = System.Threading.Tasks.Task;

namespace StudentWorkforceManagement.IntegrationTests.Persistence;

public sealed class PersistenceInterceptorTests
{
    [Fact]
    public async ThreadingTask Auditable_interceptor_sets_created_and_updated_timestamps()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero));
        await using var context = CreateContext(clock);
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Research"
        };

        context.Skills.Add(skill);
        await context.SaveChangesAsync();

        Assert.Equal(clock.UtcNow, skill.CreatedAt);
        Assert.Equal(clock.UtcNow, skill.UpdatedAt);

        var createdAt = skill.CreatedAt;
        clock.UtcNow = clock.UtcNow.AddHours(2);
        skill.Description = "Academic research support";
        await context.SaveChangesAsync();

        Assert.Equal(createdAt, skill.CreatedAt);
        Assert.Equal(clock.UtcNow, skill.UpdatedAt);
    }

    [Fact]
    public async ThreadingTask Concurrency_interceptor_assigns_and_rotates_guid_tokens()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero));
        await using var context = CreateContext(clock);
        var student = CreateStudent();

        context.Students.Add(student);
        await context.SaveChangesAsync();

        var originalToken = student.ConcurrencyToken;
        Assert.NotEqual(Guid.Empty, originalToken);

        student.Department = "Student Success";
        await context.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, student.ConcurrencyToken);
        Assert.NotEqual(originalToken, student.ConcurrencyToken);
    }

    [Fact]
    public async ThreadingTask Stale_concurrency_token_raises_conflict()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero));
        var studentId = Guid.NewGuid();

        await using (var setup = CreateContext(clock, databaseName))
        {
            var student = CreateStudent(studentId);
            setup.Students.Add(student);
            await setup.SaveChangesAsync();
        }

        await using var firstContext = CreateContext(clock, databaseName);
        await using var secondContext = CreateContext(clock, databaseName);
        var firstStudent = await firstContext.Students.SingleAsync(student => student.Id == studentId);
        var secondStudent = await secondContext.Students.SingleAsync(student => student.Id == studentId);

        firstStudent.FirstName = "Aylin";
        await firstContext.SaveChangesAsync();

        secondStudent.FirstName = "Deniz";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    private static ApplicationDbContext CreateContext(FakeClock clock, string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .AddInterceptors(new AuditableEntityInterceptor(clock), new ConcurrencyTokenInterceptor())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Student CreateStudent(Guid? id = null)
    {
        return new Student
        {
            Id = id ?? Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = $"ada-{Guid.NewGuid():N}@example.com",
            Department = "Operations"
        };
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IUtcClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
