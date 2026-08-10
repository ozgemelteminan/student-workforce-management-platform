using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Feedback.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Feedback.Queries;

public sealed record GetFeedbackQuery : PagedQuery, IRequest<PaginatedResult<FeedbackDto>>, IAuthorizableRequest
{
    public Guid? TaskId { get; init; }
    public Guid? StudentId { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class FeedbackQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetFeedbackQuery, PaginatedResult<FeedbackDto>>
{
    public async System.Threading.Tasks.Task<PaginatedResult<FeedbackDto>> Handle(GetFeedbackQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Feedback.AsNoTracking().AsQueryable();
        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            query = query.Where(feedback => feedback.StudentId == currentUser.RequireStudentId());
        }
        if (request.TaskId.HasValue)
        {
            query = query.Where(feedback => feedback.TaskId == request.TaskId.Value);
        }
        if (request.StudentId.HasValue)
        {
            query = query.Where(feedback => feedback.StudentId == request.StudentId.Value);
        }
        return await query.OrderByDescending(feedback => feedback.CreatedAt)
            .Select(feedback => new FeedbackDto(feedback.Id, feedback.TaskId, feedback.StudentId, feedback.CreatedById, feedback.Rating, feedback.Comment, feedback.CreatedAt))
            .ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
