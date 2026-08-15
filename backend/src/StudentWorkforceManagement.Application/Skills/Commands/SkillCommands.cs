using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Skills.DTOs;
using StudentWorkforceManagement.Domain.Enums;
using Skill = StudentWorkforceManagement.Domain.Entities.Skill;
using StudentSkill = StudentWorkforceManagement.Domain.Entities.StudentSkill;

namespace StudentWorkforceManagement.Application.Skills.Commands;

public sealed record CreateSkillCommand(string Name, string? Description) : IRequest<SkillDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record UpdateSkillCommand(Guid SkillId, string Name, string? Description) : IRequest<SkillDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record DeactivateSkillCommand(Guid SkillId) : IRequest<SkillDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record ReactivateSkillCommand(Guid SkillId) : IRequest<SkillDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record UpsertStudentSkillCommand(Guid StudentId, Guid SkillId, SkillLevel Level) : IRequest<StudentSkillDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class CreateSkillCommandValidator : AbstractValidator<CreateSkillCommand>
{
    public CreateSkillCommandValidator()
    {
        RuleFor(command => command.Name).Must(value => string.IsNullOrWhiteSpace(value) == false).WithMessage("Skill name is required.").MaximumLength(120);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

public sealed class UpdateSkillCommandValidator : AbstractValidator<UpdateSkillCommand>
{
    public UpdateSkillCommandValidator()
    {
        RuleFor(command => command.SkillId).NotEmpty();
        RuleFor(command => command.Name).Must(value => string.IsNullOrWhiteSpace(value) == false).WithMessage("Skill name is required.").MaximumLength(120);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

public sealed class UpsertStudentSkillCommandValidator : AbstractValidator<UpsertStudentSkillCommand>
{
    public UpsertStudentSkillCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.SkillId).NotEmpty();
        RuleFor(command => command.Level).IsInEnum();
    }
}

public sealed class SkillCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<CreateSkillCommand, SkillDto>,
      IRequestHandler<UpdateSkillCommand, SkillDto>,
      IRequestHandler<DeactivateSkillCommand, SkillDto>,
      IRequestHandler<ReactivateSkillCommand, SkillDto>,
      IRequestHandler<UpsertStudentSkillCommand, StudentSkillDto>
{
    public async System.Threading.Tasks.Task<SkillDto> Handle(CreateSkillCommand request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        await EnsureNameAvailableAsync(name, null, cancellationToken);
        var skill = new Skill { Id = Guid.NewGuid(), Name = name, Description = CleanDescription(request.Description), IsActive = true };
        dbContext.Skills.Add(skill);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(skill);
    }

    public async System.Threading.Tasks.Task<SkillDto> Handle(UpdateSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await dbContext.Skills.SingleOrDefaultAsync(item => item.Id == request.SkillId, cancellationToken)
            ?? throw new NotFoundException("Skill", request.SkillId);
        var name = NormalizeName(request.Name);
        await EnsureNameAvailableAsync(name, request.SkillId, cancellationToken);
        skill.Name = name;
        skill.Description = CleanDescription(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(skill);
    }

    public async System.Threading.Tasks.Task<SkillDto> Handle(DeactivateSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await dbContext.Skills.SingleOrDefaultAsync(item => item.Id == request.SkillId, cancellationToken)
            ?? throw new NotFoundException("Skill", request.SkillId);
        skill.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(skill);
    }

    public async System.Threading.Tasks.Task<SkillDto> Handle(ReactivateSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await dbContext.Skills.SingleOrDefaultAsync(item => item.Id == request.SkillId, cancellationToken)
            ?? throw new NotFoundException("Skill", request.SkillId);
        await EnsureNameAvailableAsync(skill.Name, skill.Id, cancellationToken);
        skill.IsActive = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(skill);
    }

    public async System.Threading.Tasks.Task<StudentSkillDto> Handle(UpsertStudentSkillCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new ForbiddenException("Students may update only their own skills.");
        }

        if (!await dbContext.Skills.AnyAsync(skill => skill.Id == request.SkillId && skill.IsActive, cancellationToken))
        {
            throw new ConflictException("Inactive or missing skills cannot be selected.");
        }

        var entity = await dbContext.StudentSkills.SingleOrDefaultAsync(skill => skill.StudentId == request.StudentId && skill.SkillId == request.SkillId, cancellationToken);
        if (entity is null)
        {
            entity = new StudentSkill { Id = Guid.NewGuid(), StudentId = request.StudentId, SkillId = request.SkillId };
            dbContext.StudentSkills.Add(entity);
        }

        entity.Level = request.Level;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new StudentSkillDto(entity.Id, entity.StudentId, entity.SkillId, entity.Level);
    }

    private async System.Threading.Tasks.Task EnsureNameAvailableAsync(string name, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalized = name.ToUpperInvariant();
        var exists = await dbContext.Skills.AnyAsync(skill =>
            skill.Name.ToUpper() == normalized && (!currentId.HasValue || skill.Id != currentId.Value), cancellationToken);
        if (exists)
        {
            throw new ConflictException("Skill name already exists.");
        }
    }

    private static string NormalizeName(string name) => name.Trim();

    private static string? CleanDescription(string? description) => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static SkillDto ToDto(Skill skill) => new(skill.Id, skill.Name, skill.Description, skill.IsActive);
}
