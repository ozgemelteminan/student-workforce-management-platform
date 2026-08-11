using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Skills.DTOs;

public sealed record SkillDto(Guid Id, string Name, string? Description);
public sealed record StudentSkillDto(Guid Id, Guid StudentId, Guid SkillId, SkillLevel Level);
public sealed record StudentSkillDetailDto(Guid SkillId, string Name, SkillLevel Level);
