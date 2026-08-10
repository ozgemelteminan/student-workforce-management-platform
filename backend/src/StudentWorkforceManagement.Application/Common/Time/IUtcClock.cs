namespace StudentWorkforceManagement.Application.Common.Time;

public interface IUtcClock
{
    DateTimeOffset UtcNow { get; }
}
