using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskDependency : AuditableEntity
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid DependsOnTaskId { get; set; }
    public Task? DependsOnTask { get; set; }
}
