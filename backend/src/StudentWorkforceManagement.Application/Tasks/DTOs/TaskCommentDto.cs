using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Tasks.DTOs;

public sealed record TaskCommentDto(Guid Id, Guid TaskId, Guid AuthorId, string Content, TaskCommentVisibility Visibility, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
