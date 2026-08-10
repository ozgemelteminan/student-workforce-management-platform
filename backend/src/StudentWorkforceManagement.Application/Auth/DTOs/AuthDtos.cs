using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Auth.DTOs;

public sealed record InvitationDto(Guid Id, string Email, DateTimeOffset ExpiresAt, DateTimeOffset? AcceptedAt, DateTimeOffset? RevokedAt, Guid? CreatedById, DateTimeOffset CreatedAt);

public sealed record CreatedInvitationDto(Guid Id, string Email, DateTimeOffset ExpiresAt, string RawToken);

public sealed record SessionDto(Guid Id, Guid UserId, string? DeviceName, string? IpAddress, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt, DateTimeOffset CreatedAt);

public sealed record RefreshTokenRotationDto(Guid SessionId, string RawRefreshToken, DateTimeOffset ExpiresAt);

public sealed record AuthenticationResultDto(Guid UserId, Guid SessionId, string Email, string DisplayName, IReadOnlyCollection<string> Roles, string AccessToken, string RawRefreshToken, DateTimeOffset SessionExpiresAt, DateTimeOffset RefreshTokenExpiresAt);

public sealed record PasswordResetRequestDto(string Email, DateTimeOffset ExpiresAt);

public sealed record PasswordResetResultDto(Guid UserId, DateTimeOffset ConsumedAt, int RevokedSessionCount);

public sealed record AuthContractGapDto(string Feature, string MissingPersistenceCapability, string MinimalProposedSchemaChange);
