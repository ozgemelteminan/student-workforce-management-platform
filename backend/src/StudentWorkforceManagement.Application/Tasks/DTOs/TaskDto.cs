using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Tasks.DTOs;

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    Guid CategoryId,
    Guid? SemesterId,
    TaskPriority Priority,
    TaskDifficulty Difficulty,
    TaskStatus Status,
    Guid CreatedById,
    Guid? AssignedStudentId,
    DateTimeOffset? StartDate,
    DateTimeOffset Deadline,
    int EstimatedDurationMinutes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    Guid ConcurrencyToken);
