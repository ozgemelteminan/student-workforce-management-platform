using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Requests.DTOs;

public sealed record TaskRequestDto(
    Guid Id,
    Guid TaskId,
    Guid RequestedById,
    RequestType Type,
    string Reason,
    DateTimeOffset? CurrentDeadline,
    DateTimeOffset? RequestedDeadline,
    Guid? SuggestedStudentId,
    RequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedById,
    string? ReviewerComment,
    Guid ConcurrencyToken,
    string? TaskTitle = null,
    string? RequestedByName = null,
    string? SuggestedStudentName = null,
    string? ReviewedByName = null);
