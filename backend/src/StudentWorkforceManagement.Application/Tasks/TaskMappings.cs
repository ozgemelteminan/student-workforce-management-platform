using StudentWorkforceManagement.Application.Tasks.DTOs;
using DomainTask = StudentWorkforceManagement.Domain.Entities.Task;

namespace StudentWorkforceManagement.Application.Tasks;

internal static class TaskMappings
{
    public static TaskDto ToDto(DomainTask task)
    {
        return new TaskDto(task.Id, task.Title, task.Description, task.CategoryId, task.SemesterId, task.Priority, task.Difficulty, task.Status, task.CreatedById, task.AssignedStudentId, task.StartDate, task.Deadline, task.EstimatedDurationMinutes, task.CreatedAt, task.UpdatedAt, task.CompletedAt, task.ConcurrencyToken, task.Category?.Name, task.AssignedStudent is null ? null : $"{task.AssignedStudent.FirstName} {task.AssignedStudent.LastName}".Trim(), task.CreatedBy?.DisplayName);
    }
}
