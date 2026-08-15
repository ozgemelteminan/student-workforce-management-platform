using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Skills.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Skills.Queries.GetSkills;

public sealed record GetSkillsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyCollection<SkillDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetSkillQuery(Guid SkillId) : IRequest<SkillDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetSkillsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetSkillsQuery, IReadOnlyCollection<SkillDto>>
    , IRequestHandler<GetSkillQuery, SkillDto>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<SkillDto>> Handle(GetSkillsQuery request, CancellationToken cancellationToken)
    {
        if (request.IncludeInactive && !currentUser.IsInRole(UserRole.ADMIN))
        {
            throw new ForbiddenException();
        }

        var query = dbContext.Skills.AsNoTracking();
        if (!request.IncludeInactive)
        {
            query = query.Where(skill => skill.IsActive);
        }

        return await query.OrderBy(skill => skill.Name).Select(skill => new SkillDto(skill.Id, skill.Name, skill.Description, skill.IsActive)).ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<SkillDto> Handle(GetSkillQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Skills.AsNoTracking()
            .Where(skill => skill.Id == request.SkillId)
            .Select(skill => new SkillDto(skill.Id, skill.Name, skill.Description, skill.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Skill", request.SkillId);
    }
}
