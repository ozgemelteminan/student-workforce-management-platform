namespace StudentWorkforceManagement.Application.Feedback.DTOs;

public sealed record FeedbackDto(Guid Id, Guid TaskId, Guid StudentId, Guid CreatedById, int? Rating, string? Comment, DateTimeOffset CreatedAt);
