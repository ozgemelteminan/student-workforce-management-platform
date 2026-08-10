using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskAssignmentHistory : Entity, IHasConcurrencyToken
{
    public Guid TaskId { get; set; }
    public Guid StudentId { get; set; }
    public Guid AssignedById { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? UnassignedAt { get; set; }
    public AssignmentStatus AssignmentStatus { get; set; }
    public string? Reason { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
