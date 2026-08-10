namespace StudentWorkforceManagement.Application.RecurringTasks.DTOs;

public sealed record RecurringTaskDto(Guid Id, Guid TemplateId, string Frequency, string TimeZoneId, TimeOnly? LocalRunTime, DateTimeOffset NextRunAt, bool IsActive, Guid CreatedById, Guid ConcurrencyToken, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
