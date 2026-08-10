namespace StudentWorkforceManagement.Application.Settings.DTOs;

public sealed record SystemSettingDto(Guid Id, string Key, string Value, string? Description, Guid ConcurrencyToken);
