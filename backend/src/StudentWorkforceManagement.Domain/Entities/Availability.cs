using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Availability : Entity, IHasConcurrencyToken
{
    public Guid StudentId { get; set; }
    public Guid? SemesterId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
