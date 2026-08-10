using StudentWorkforceManagement.Application.Common.Time;

namespace StudentWorkforceManagement.Infrastructure.Time;

public sealed class UtcClock : IUtcClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
