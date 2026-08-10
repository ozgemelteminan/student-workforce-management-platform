using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.Persistence;
using StudentWorkforceManagement.Infrastructure.Persistence.Interceptors;
using ThreadingTask = System.Threading.Tasks.Task;

namespace StudentWorkforceManagement.IntegrationTests.Persistence;

public sealed class PersistenceRoundTripTests
{
    [Fact]
    public async ThreadingTask Task_assignment_history_and_task_request_round_trip_through_dbcontext()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var taskId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        await using (var context = CreateContext(databaseName))
        {
            context.TaskAssignmentHistory.Add(new TaskAssignmentHistory
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                StudentId = studentId,
                AssignedByUserId = actorId,
                AssignedAt = new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero),
                Status = AssignmentStatus.ACTIVE,
                Mode = AssignmentMode.MANUAL,
                IsActive = true,
                Reason = "Initial assignment"
            });

            context.TaskRequests.Add(new TaskRequest
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                RequestedById = studentId,
                Type = RequestType.EXTENSION,
                Reason = "Need more time",
                CurrentDeadline = new DateTimeOffset(2026, 8, 17, 17, 0, 0, TimeSpan.Zero),
                RequestedDeadline = new DateTimeOffset(2026, 8, 18, 17, 0, 0, TimeSpan.Zero),
                Status = RequestStatus.PENDING
            });

            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(databaseName))
        {
            var assignment = await context.TaskAssignmentHistory.SingleAsync(entity => entity.TaskId == taskId);
            var request = await context.TaskRequests.SingleAsync(entity => entity.TaskId == taskId);

            Assert.Equal(studentId, assignment.StudentId);
            Assert.Equal(AssignmentStatus.ACTIVE, assignment.Status);
            Assert.Equal(RequestType.EXTENSION, request.Type);
            Assert.Equal(RequestStatus.PENDING, request.Status);
            Assert.NotEqual(Guid.Empty, assignment.ConcurrencyToken);
            Assert.NotEqual(Guid.Empty, request.ConcurrencyToken);
        }
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(
                new AuditableEntityInterceptor(new FakeClock()),
                new ConcurrencyTokenInterceptor())
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class FakeClock : IUtcClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 8, 10, 10, 0, 0, TimeSpan.Zero);
    }
}
