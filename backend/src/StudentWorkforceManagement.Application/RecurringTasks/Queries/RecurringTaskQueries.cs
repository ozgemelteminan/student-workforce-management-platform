using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.RecurringTasks.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.RecurringTasks.Queries;

public sealed record GetRecurringTasksQuery : PagedQuery, IRequest<PaginatedResult<RecurringTaskDto>>, IAuthorizableRequest
{
    public bool? IsActive { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record GetRecurringTaskQuery(Guid RecurringTaskId) : IRequest<RecurringTaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class RecurringTaskQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext)
    : IRequestHandler<GetRecurringTasksQuery, PaginatedResult<RecurringTaskDto>>, IRequestHandler<GetRecurringTaskQuery, RecurringTaskDto>
{
    public async System.Threading.Tasks.Task<PaginatedResult<RecurringTaskDto>> Handle(GetRecurringTasksQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.RecurringTasks.AsNoTracking().AsQueryable();
        if (request.IsActive.HasValue) query = query.Where(recurring => recurring.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(recurring => recurring.Frequency.ToLower().Contains(term) || recurring.TimeZoneId.ToLower().Contains(term));
        }
        return await query.OrderBy(recurring => recurring.NextRunAt).Select(recurring => new RecurringTaskDto(recurring.Id, recurring.TemplateId, recurring.Frequency, recurring.TimeZoneId, recurring.LocalRunTime, recurring.NextRunAt, recurring.IsActive, recurring.CreatedById, recurring.ConcurrencyToken, recurring.CreatedAt, recurring.UpdatedAt)).ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async System.Threading.Tasks.Task<RecurringTaskDto> Handle(GetRecurringTaskQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.RecurringTasks.AsNoTracking().Where(recurring => recurring.Id == request.RecurringTaskId).Select(recurring => new RecurringTaskDto(recurring.Id, recurring.TemplateId, recurring.Frequency, recurring.TimeZoneId, recurring.LocalRunTime, recurring.NextRunAt, recurring.IsActive, recurring.CreatedById, recurring.ConcurrencyToken, recurring.CreatedAt, recurring.UpdatedAt)).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("RecurringTask", request.RecurringTaskId);
    }
}
