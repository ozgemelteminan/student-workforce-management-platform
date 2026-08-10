using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Interfaces;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public sealed class TaskDependencyService(IApplicationDbContext dbContext) : ITaskDependencyService
{
    public async System.Threading.Tasks.Task<bool> WouldCreateCycleAsync(Guid taskId, Guid dependsOnTaskId, CancellationToken cancellationToken = default)
    {
        if (taskId == dependsOnTaskId)
        {
            return true;
        }

        var edges = await dbContext.TaskDependencies.AsNoTracking()
            .Select(dependency => new { dependency.TaskId, dependency.DependsOnTaskId })
            .ToListAsync(cancellationToken);

        var dependenciesByTask = edges
            .GroupBy(edge => edge.TaskId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.DependsOnTaskId).ToArray());

        var stack = new Stack<Guid>();
        var visited = new HashSet<Guid>();
        stack.Push(dependsOnTaskId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            if (current == taskId)
            {
                return true;
            }

            if (dependenciesByTask.TryGetValue(current, out var nextDependencies))
            {
                foreach (var next in nextDependencies)
                {
                    stack.Push(next);
                }
            }
        }

        return false;
    }
}
