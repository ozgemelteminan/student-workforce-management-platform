using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Templates.DTOs;

public sealed record TaskTemplateDto(Guid Id, string Title, string? Description, Guid CategoryId, TaskPriority DefaultPriority, TaskDifficulty DefaultDifficulty, int EstimatedDurationMinutes, Guid CreatedById, string? ChecklistTemplateJson, string? RequiredSkillsTemplateJson, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
