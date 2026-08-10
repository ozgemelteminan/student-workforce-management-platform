using MediatR;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Application.Tasks.Queries.GetTasks;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Queries.GetMyTasks;

public sealed record GetMyTasksQuery : PagedQuery, IRequest<PaginatedResult<TaskDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed class GetMyTasksQueryHandler(IMediator mediator, ICurrentUserService currentUser) : IRequestHandler<GetMyTasksQuery, PaginatedResult<TaskDto>>
{
    public System.Threading.Tasks.Task<PaginatedResult<TaskDto>> Handle(GetMyTasksQuery request, CancellationToken cancellationToken)
    {
        return mediator.Send(new GetTasksQuery
        {
            StudentId = currentUser.RequireStudentId(),
            Page = request.Page,
            PageSize = request.PageSize,
            Search = request.Search,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection
        }, cancellationToken);
    }
}
