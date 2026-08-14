using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Queries.GetTaskSubresources;

public sealed record GetTaskAssignmentHistoryQuery(Guid TaskId) : IRequest<IReadOnlyCollection<TaskAssignmentHistoryDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record GetTaskDependenciesQuery(Guid TaskId) : IRequest<IReadOnlyCollection<TaskDependencyDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetTaskRequiredSkillsQuery(Guid TaskId) : IRequest<IReadOnlyCollection<TaskRequiredSkillDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetTaskCommentsQuery(Guid TaskId) : IRequest<IReadOnlyCollection<TaskCommentDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetTaskChecklistQuery(Guid TaskId) : IRequest<IReadOnlyCollection<TaskChecklistItemDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetTaskSubresourceQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetTaskAssignmentHistoryQuery, IReadOnlyCollection<TaskAssignmentHistoryDto>>,
      IRequestHandler<GetTaskDependenciesQuery, IReadOnlyCollection<TaskDependencyDto>>,
      IRequestHandler<GetTaskRequiredSkillsQuery, IReadOnlyCollection<TaskRequiredSkillDto>>,
      IRequestHandler<GetTaskCommentsQuery, IReadOnlyCollection<TaskCommentDto>>,
      IRequestHandler<GetTaskChecklistQuery, IReadOnlyCollection<TaskChecklistItemDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<TaskAssignmentHistoryDto>> Handle(GetTaskAssignmentHistoryQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TaskAssignmentHistory.AsNoTracking()
            .Where(history => history.TaskId == request.TaskId)
            .OrderByDescending(history => history.AssignedAt)
            .Select(history => new TaskAssignmentHistoryDto(history.Id, history.TaskId, history.StudentId, history.AssignedByUserId, history.AssignedAt, history.UnassignedAt, history.Status, history.Mode, history.IsActive, history.PlannedEffortMinutes, history.Reason))
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<TaskDependencyDto>> Handle(GetTaskDependenciesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TaskDependencies.AsNoTracking()
            .Where(dependency => dependency.TaskId == request.TaskId)
            .Select(dependency => new TaskDependencyDto(dependency.Id, dependency.TaskId, dependency.DependsOnTaskId))
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<TaskRequiredSkillDto>> Handle(GetTaskRequiredSkillsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TaskRequiredSkills.AsNoTracking()
            .Where(skill => skill.TaskId == request.TaskId)
            .Select(skill => new TaskRequiredSkillDto(skill.Id, skill.TaskId, skill.SkillId, skill.Skill == null ? null : skill.Skill.Name, skill.MinimumLevel))
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<TaskCommentDto>> Handle(GetTaskCommentsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.TaskComments.AsNoTracking().Where(comment => comment.TaskId == request.TaskId);
        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            query = query.Where(comment => comment.Visibility == TaskCommentVisibility.STUDENT_VISIBLE);
        }

        return await query.OrderBy(comment => comment.CreatedAt)
            .Select(comment => new TaskCommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.Content, comment.Visibility, comment.CreatedAt, comment.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<TaskChecklistItemDto>> Handle(GetTaskChecklistQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TaskChecklistItems.AsNoTracking()
            .Where(item => item.TaskId == request.TaskId)
            .OrderBy(item => item.Order)
            .Select(item => new TaskChecklistItemDto(item.Id, item.TaskId, item.Title, item.IsCompleted, item.CompletedAt, item.CompletedById, item.Order))
            .ToListAsync(cancellationToken);
    }
}
