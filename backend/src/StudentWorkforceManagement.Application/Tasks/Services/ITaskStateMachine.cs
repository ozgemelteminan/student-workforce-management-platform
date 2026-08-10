using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public interface ITaskStateMachine
{
    void ValidateTransition(TaskStatus currentStatus, TaskStatus targetStatus, IReadOnlyCollection<UserRole> actorRoles, bool isAssignedStudent, bool isSelfReview = false);
}
