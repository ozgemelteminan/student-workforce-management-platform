using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskDependency : Entity
{
    public Guid TaskId { get; set; }
    public Guid DependsOnTaskId { get; set; }
}
