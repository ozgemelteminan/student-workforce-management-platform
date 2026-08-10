using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.DTOs;

public sealed record TaskRequiredSkillDto(Guid Id, Guid TaskId, Guid SkillId, string? SkillName, SkillLevel MinimumLevel);
