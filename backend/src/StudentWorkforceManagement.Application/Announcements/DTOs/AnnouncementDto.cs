namespace StudentWorkforceManagement.Application.Announcements.DTOs;

public sealed record AnnouncementDto(Guid Id, string Title, string Content, Guid CreatedById, DateTimeOffset? ExpiresAt, bool IsPinned, bool IsPublished, DateTimeOffset? PublishedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
