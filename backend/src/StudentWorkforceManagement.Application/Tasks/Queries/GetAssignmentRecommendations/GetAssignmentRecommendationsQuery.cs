using MediatR;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Application.Tasks.Services;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.Queries.GetAssignmentRecommendations;

public sealed record GetAssignmentRecommendationsQuery(Guid TaskId) : IRequest<IReadOnlyCollection<AssignmentRecommendationDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class GetAssignmentRecommendationsQueryHandler(IAssignmentRecommendationService recommendationService)
    : IRequestHandler<GetAssignmentRecommendationsQuery, IReadOnlyCollection<AssignmentRecommendationDto>>
{
    public System.Threading.Tasks.Task<IReadOnlyCollection<AssignmentRecommendationDto>> Handle(GetAssignmentRecommendationsQuery request, CancellationToken cancellationToken)
    {
        return recommendationService.GetRecommendationsAsync(request.TaskId, cancellationToken);
    }
}
