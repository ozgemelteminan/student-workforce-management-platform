namespace StudentWorkforceManagement.Application.Audit.DTOs;

public sealed record AuditLogDto(Guid Id, Guid? UserId, string Action, string EntityType, Guid? EntityId, string? OldValue, string? NewValue, string? IpAddress, string? CorrelationId, DateTimeOffset CreatedAt, string? ActorDisplayName = null, string? ActorEmail = null);
