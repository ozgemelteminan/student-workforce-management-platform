using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.DTOs;

public sealed record TaskAssignmentHistoryDto(
    Guid Id,
    Guid TaskId,
    Guid StudentId,
    Guid AssignedByUserId,
    DateTimeOffset AssignedAt,
    DateTimeOffset? UnassignedAt,
    AssignmentStatus Status,
    AssignmentMode Mode,
    bool IsActive,
    string? Reason);
