namespace StudentWorkforceManagement.Application.Tasks.DTOs;

public sealed record TaskChecklistItemDto(Guid Id, Guid TaskId, string Title, bool IsCompleted, DateTimeOffset? CompletedAt, Guid? CompletedById, int Order);
