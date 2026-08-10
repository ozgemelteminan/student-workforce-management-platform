using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Skills.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Skills.Queries.GetSkills;

public sealed record GetSkillsQuery : IRequest<IReadOnlyCollection<SkillDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetSkillsQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext) : IRequestHandler<GetSkillsQuery, IReadOnlyCollection<SkillDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<SkillDto>> Handle(GetSkillsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Skills.AsNoTracking().OrderBy(skill => skill.Name).Select(skill => new SkillDto(skill.Id, skill.Name, skill.Description)).ToListAsync(cancellationToken);
    }
}
