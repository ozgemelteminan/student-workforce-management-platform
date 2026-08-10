using StudentWorkforceManagement.Application.Tasks.DTOs;

namespace StudentWorkforceManagement.Application.Tasks.Services;

public interface IAssignmentRecommendationService
{
    System.Threading.Tasks.Task<IReadOnlyCollection<AssignmentRecommendationDto>> GetRecommendationsAsync(Guid taskId, CancellationToken cancellationToken = default);
}
