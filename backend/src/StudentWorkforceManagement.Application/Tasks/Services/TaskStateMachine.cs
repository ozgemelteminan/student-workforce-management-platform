using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public sealed class TaskStateMachine : ITaskStateMachine
{
    public void ValidateTransition(TaskStatus currentStatus, TaskStatus targetStatus, IReadOnlyCollection<UserRole> actorRoles, bool isAssignedStudent, bool isSelfReview = false)
    {
        if (!IsAllowedTransition(currentStatus, targetStatus))
        {
            throw new InvalidStateTransitionException(currentStatus, targetStatus);
        }

        if (IsStudentWorkflow(targetStatus) && !isAssignedStudent)
        {
            throw new ForbiddenException("Only the assigned student may perform this task transition.");
        }

        if (targetStatus is TaskStatus.COMPLETED && isSelfReview)
        {
            throw new ForbiddenException("A user cannot approve their own submitted work.");
        }

        if (targetStatus is TaskStatus.COMPLETED or TaskStatus.IN_PROGRESS && currentStatus is TaskStatus.SUBMITTED_FOR_REVIEW)
        {
            if (!actorRoles.Contains(UserRole.ADMIN) && !actorRoles.Contains(UserRole.REVIEWER))
            {
                throw new ForbiddenException("Submission review requires ADMIN or REVIEWER authority.");
            }
        }

        if (targetStatus is TaskStatus.CANCELLED && !actorRoles.Contains(UserRole.ADMIN) && !actorRoles.Contains(UserRole.TASK_MANAGER))
        {
            throw new ForbiddenException("Only ADMIN or TASK_MANAGER may cancel a task.");
        }
    }

    private static bool IsStudentWorkflow(TaskStatus targetStatus)
    {
        return targetStatus is TaskStatus.ACCEPTED or TaskStatus.IN_PROGRESS or TaskStatus.SUBMITTED_FOR_REVIEW;
    }

    private static bool IsAllowedTransition(TaskStatus currentStatus, TaskStatus targetStatus)
    {
        return (currentStatus, targetStatus) switch
        {
            (TaskStatus.ASSIGNED, TaskStatus.ACCEPTED) => true,
            (TaskStatus.ACCEPTED, TaskStatus.IN_PROGRESS) => true,
            (TaskStatus.IN_PROGRESS, TaskStatus.SUBMITTED_FOR_REVIEW) => true,
            (TaskStatus.SUBMITTED_FOR_REVIEW, TaskStatus.COMPLETED) => true,
            (TaskStatus.SUBMITTED_FOR_REVIEW, TaskStatus.IN_PROGRESS) => true,
            (TaskStatus.ASSIGNED, TaskStatus.CANCELLED) => true,
            (TaskStatus.ACCEPTED, TaskStatus.CANCELLED) => true,
            (TaskStatus.IN_PROGRESS, TaskStatus.CANCELLED) => true,
            _ => false
        };
    }
}
