using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Analytics.DTOs;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Analytics.Queries;

public sealed record GetDashboardAnalyticsQuery : IRequest<DashboardAnalyticsDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record GetTasksByStatusAnalyticsQuery : IRequest<IReadOnlyCollection<TasksByStatusDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record GetTasksByCategoryAnalyticsQuery : IRequest<IReadOnlyCollection<TasksByCategoryDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record GetWorkloadDistributionQuery : IRequest<IReadOnlyCollection<WorkloadDistributionDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record GetRequestAnalyticsQuery : IRequest<IReadOnlyCollection<RequestAnalyticsDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class AnalyticsQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, IUtcClock clock)
    : IRequestHandler<GetDashboardAnalyticsQuery, DashboardAnalyticsDto>, IRequestHandler<GetTasksByStatusAnalyticsQuery, IReadOnlyCollection<TasksByStatusDto>>, IRequestHandler<GetTasksByCategoryAnalyticsQuery, IReadOnlyCollection<TasksByCategoryDto>>, IRequestHandler<GetWorkloadDistributionQuery, IReadOnlyCollection<WorkloadDistributionDto>>, IRequestHandler<GetRequestAnalyticsQuery, IReadOnlyCollection<RequestAnalyticsDto>>
{
    public async System.Threading.Tasks.Task<DashboardAnalyticsDto> Handle(GetDashboardAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var total = await dbContext.Tasks.CountAsync(cancellationToken);
        var active = await dbContext.Tasks.CountAsync(task => task.Status != TaskStatus.COMPLETED && task.Status != TaskStatus.CANCELLED, cancellationToken);
        var completed = await dbContext.Tasks.CountAsync(task => task.Status == TaskStatus.COMPLETED, cancellationToken);
        var overdue = await dbContext.Tasks.CountAsync(task => task.Deadline < clock.UtcNow && task.Status != TaskStatus.COMPLETED && task.Status != TaskStatus.CANCELLED, cancellationToken);
        var pendingReviews = await dbContext.TaskSubmissions.CountAsync(submission => submission.Status == SubmissionStatus.SUBMITTED_FOR_REVIEW, cancellationToken);
        var pendingRequests = await dbContext.TaskRequests.CountAsync(item => item.Status == RequestStatus.PENDING, cancellationToken);
        return new DashboardAnalyticsDto(total, active, completed, overdue, pendingReviews, pendingRequests);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<TasksByStatusDto>> Handle(GetTasksByStatusAnalyticsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Tasks.AsNoTracking().GroupBy(task => task.Status).Select(group => new TasksByStatusDto(group.Key, group.Count())).ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<TasksByCategoryDto>> Handle(GetTasksByCategoryAnalyticsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Tasks.AsNoTracking().GroupBy(task => new { task.CategoryId, task.Category!.Name }).Select(group => new TasksByCategoryDto(group.Key.CategoryId, group.Key.Name, group.Count())).ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<WorkloadDistributionDto>> Handle(GetWorkloadDistributionQuery request, CancellationToken cancellationToken)
    {
        var activeStatuses = new[] { TaskStatus.ASSIGNED, TaskStatus.ACCEPTED, TaskStatus.IN_PROGRESS, TaskStatus.SUBMITTED_FOR_REVIEW, TaskStatus.OVERDUE };
        return await dbContext.Students.AsNoTracking()
            .Select(student => new WorkloadDistributionDto(
                student.Id,
                student.FirstName + " " + student.LastName,
                dbContext.Tasks.Where(task => task.AssignedStudentId == student.Id && activeStatuses.Contains(task.Status)).Sum(task => (int?)task.EstimatedDurationMinutes) ?? 0,
                dbContext.Tasks.Count(task => task.AssignedStudentId == student.Id && activeStatuses.Contains(task.Status))))
            .OrderByDescending(item => item.ActiveWorkloadMinutes)
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<RequestAnalyticsDto>> Handle(GetRequestAnalyticsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TaskRequests.AsNoTracking().GroupBy(item => new { item.Type, item.Status }).Select(group => new RequestAnalyticsDto(group.Key.Type, group.Key.Status, group.Count())).ToListAsync(cancellationToken);
    }
}
