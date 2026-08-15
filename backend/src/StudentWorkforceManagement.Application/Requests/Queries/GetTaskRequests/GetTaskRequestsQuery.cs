using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Requests.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Requests.Queries.GetTaskRequests;

public sealed record GetTaskRequestsQuery : PagedQuery, IRequest<PaginatedResult<TaskRequestDto>>, IAuthorizableRequest
{
    public Guid? TaskId { get; init; }
    public RequestType? Type { get; init; }
    public RequestStatus? Status { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetTaskRequestsQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetTaskRequestsQuery, PaginatedResult<TaskRequestDto>>
{
    public async System.Threading.Tasks.Task<PaginatedResult<TaskRequestDto>> Handle(GetTaskRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.TaskRequests.AsNoTracking().AsQueryable();
        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            query = query.Where(entity => entity.RequestedById == currentUser.RequireStudentId());
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(entity =>
                entity.Reason.ToLower().Contains(search)
                || entity.Task!.Title.ToLower().Contains(search)
                || entity.RequestedBy!.FirstName.ToLower().Contains(search)
                || entity.RequestedBy.LastName.ToLower().Contains(search));
        }
        if (request.TaskId.HasValue) query = query.Where(entity => entity.TaskId == request.TaskId.Value);
        if (request.Type.HasValue) query = query.Where(entity => entity.Type == request.Type.Value);
        if (request.Status.HasValue) query = query.Where(entity => entity.Status == request.Status.Value);

        return await query.OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new TaskRequestDto(
                entity.Id,
                entity.TaskId,
                entity.RequestedById,
                entity.Type,
                entity.Reason,
                entity.CurrentDeadline,
                entity.RequestedDeadline,
                entity.SuggestedStudentId,
                entity.Status,
                entity.CreatedAt,
                entity.ReviewedAt,
                entity.ReviewedById,
                entity.ReviewerComment,
                entity.ConcurrencyToken,
                entity.Task == null ? null : entity.Task.Title,
                entity.RequestedBy == null ? null : (entity.RequestedBy.FirstName + " " + entity.RequestedBy.LastName).Trim(),
                entity.SuggestedStudent == null ? null : (entity.SuggestedStudent.FirstName + " " + entity.SuggestedStudent.LastName).Trim(),
                entity.ReviewedBy == null ? null : entity.ReviewedBy.DisplayName))
            .ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
