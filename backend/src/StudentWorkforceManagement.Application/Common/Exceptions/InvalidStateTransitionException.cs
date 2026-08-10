using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Common.Exceptions;

public sealed class InvalidStateTransitionException(TaskStatus from, TaskStatus to)
    : ConflictException($"Task status cannot transition from {from} to {to}.")
{
    public TaskStatus From { get; } = from;
    public TaskStatus To { get; } = to;
}
