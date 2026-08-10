using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Availability.DTOs;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Availability.Queries.GetAvailability;

public sealed record GetStudentAvailabilityQuery(Guid StudentId, Guid? SemesterId = null) : IRequest<IReadOnlyCollection<AvailabilityDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class GetStudentAvailabilityQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetStudentAvailabilityQuery, IReadOnlyCollection<AvailabilityDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<AvailabilityDto>> Handle(GetStudentAvailabilityQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new StudentWorkforceManagement.Application.Common.Exceptions.ForbiddenException("Students may view only their own availability.");
        }
        var query = dbContext.Availability.AsNoTracking().Where(item => item.StudentId == request.StudentId);
        if (request.SemesterId.HasValue) query = query.Where(item => item.SemesterId == request.SemesterId.Value);
        return await query.OrderBy(item => item.DayOfWeek).ThenBy(item => item.StartTime).Select(item => new AvailabilityDto(item.Id, item.StudentId, item.SemesterId, item.DayOfWeek, item.StartTime, item.EndTime, item.Status, item.Reason, item.ConcurrencyToken)).ToListAsync(cancellationToken);
    }
}
