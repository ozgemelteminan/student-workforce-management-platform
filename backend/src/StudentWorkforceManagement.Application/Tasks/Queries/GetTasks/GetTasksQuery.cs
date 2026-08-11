using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Tasks.Queries.GetTasks;

public sealed record GetTasksQuery : PagedQuery, IRequest<PaginatedResult<TaskDto>>, IAuthorizableRequest
{
    public Guid? StudentId { get; init; }
    public TaskStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public TaskDifficulty? Difficulty { get; init; }
    public Guid? CategoryId { get; init; }
    public bool? IsAssigned { get; init; }
    public DateTimeOffset? DeadlineFrom { get; init; }
    public DateTimeOffset? DeadlineTo { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    private static readonly string[] AllowedSorts = ["deadline", "priority", "created", "workload"];

    public GetTasksQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.SortBy).Must(value => string.IsNullOrWhiteSpace(value) || AllowedSorts.Contains(value, StringComparer.OrdinalIgnoreCase)).WithMessage("Unsupported task sort field.");
        RuleFor(query => query.SortDirection).Must(value => string.IsNullOrWhiteSpace(value) || value.Equals("asc", StringComparison.OrdinalIgnoreCase) || value.Equals("desc", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class GetTasksQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetTasksQuery, PaginatedResult<TaskDto>>
{
    public async System.Threading.Tasks.Task<PaginatedResult<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Tasks.AsNoTracking().AsQueryable();

        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            var studentId = currentUser.RequireStudentId();
            query = query.Where(task => task.AssignedStudentId == studentId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(task => task.Title.ToLower().Contains(search) || (task.Description != null && task.Description.ToLower().Contains(search)));
        }

        if (request.StudentId.HasValue) query = query.Where(task => task.AssignedStudentId == request.StudentId.Value);
        if (request.Status.HasValue) query = query.Where(task => task.Status == request.Status.Value);
        if (request.Priority.HasValue) query = query.Where(task => task.Priority == request.Priority.Value);
        if (request.Difficulty.HasValue) query = query.Where(task => task.Difficulty == request.Difficulty.Value);
        if (request.CategoryId.HasValue) query = query.Where(task => task.CategoryId == request.CategoryId.Value);
        if (request.IsAssigned.HasValue) query = request.IsAssigned.Value ? query.Where(task => task.AssignedStudentId != null) : query.Where(task => task.AssignedStudentId == null);
        if (request.DeadlineFrom.HasValue) query = query.Where(task => task.Deadline >= request.DeadlineFrom.Value);
        if (request.DeadlineTo.HasValue) query = query.Where(task => task.Deadline <= request.DeadlineTo.Value);

        query = (request.SortBy?.ToLowerInvariant(), request.SortDirection?.ToLowerInvariant()) switch
        {
            ("priority", "desc") => query.OrderByDescending(task => task.Priority).ThenBy(task => task.Deadline),
            ("priority", _) => query.OrderBy(task => task.Priority).ThenBy(task => task.Deadline),
            ("created", "desc") => query.OrderByDescending(task => task.CreatedAt),
            ("created", _) => query.OrderBy(task => task.CreatedAt),
            ("workload", "desc") => query.OrderByDescending(task => task.EstimatedDurationMinutes),
            ("workload", _) => query.OrderBy(task => task.EstimatedDurationMinutes),
            ("deadline", "desc") => query.OrderByDescending(task => task.Deadline),
            _ => query.OrderBy(task => task.Deadline)
        };

        return await query.Select(task => new TaskDto(task.Id, task.Title, task.Description, task.CategoryId, task.SemesterId, task.Priority, task.Difficulty, task.Status, task.CreatedById, task.AssignedStudentId, task.StartDate, task.Deadline, task.EstimatedDurationMinutes, task.CreatedAt, task.UpdatedAt, task.CompletedAt, task.ConcurrencyToken))
            .ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
