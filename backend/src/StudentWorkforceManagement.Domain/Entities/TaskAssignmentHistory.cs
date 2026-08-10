using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskAssignmentHistory : AuditableEntity, IHasConcurrencyToken
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid AssignedByUserId { get; set; }
    public User? AssignedByUser { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? UnassignedAt { get; set; }
    public AssignmentStatus Status { get; set; }
    public AssignmentMode Mode { get; set; }
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
