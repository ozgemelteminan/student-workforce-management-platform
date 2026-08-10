using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public sealed class TaskWorkloadService(IApplicationDbContext dbContext) : ITaskWorkloadService
{
    private static readonly TaskStatus[] ActiveStatuses =
    {
        TaskStatus.ASSIGNED,
        TaskStatus.ACCEPTED,
        TaskStatus.IN_PROGRESS,
        TaskStatus.SUBMITTED_FOR_REVIEW,
        TaskStatus.OVERDUE
    };

    public async System.Threading.Tasks.Task<int> GetActiveWorkloadMinutesAsync(Guid studentId, Guid? semesterId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Tasks.AsNoTracking()
            .Where(task => task.AssignedStudentId == studentId && ActiveStatuses.Contains(task.Status));

        if (semesterId.HasValue)
        {
            query = query.Where(task => task.SemesterId == semesterId.Value);
        }

        return await query.SumAsync(task => task.EstimatedDurationMinutes, cancellationToken);
    }
}
