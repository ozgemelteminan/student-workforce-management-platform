using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Skills.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Skills.Queries.GetStudentSkills;

public sealed record GetStudentSkillsQuery(Guid StudentId) : IRequest<IReadOnlyCollection<StudentSkillDetailDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetStudentSkillsQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetStudentSkillsQuery, IReadOnlyCollection<StudentSkillDetailDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<StudentSkillDetailDto>> Handle(GetStudentSkillsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new ForbiddenException("Students may view only their own skills.");
        }

        var studentExists = await dbContext.Students.AsNoTracking().AnyAsync(student => student.Id == request.StudentId, cancellationToken);
        if (!studentExists)
        {
            throw new NotFoundException("Student", request.StudentId);
        }

        return await dbContext.StudentSkills.AsNoTracking()
            .Where(studentSkill => studentSkill.StudentId == request.StudentId)
            .OrderBy(studentSkill => studentSkill.Skill!.Name)
            .ThenBy(studentSkill => studentSkill.SkillId)
            .Select(studentSkill => new StudentSkillDetailDto(studentSkill.SkillId, studentSkill.Skill!.Name, studentSkill.Level))
            .ToListAsync(cancellationToken);
    }
}
