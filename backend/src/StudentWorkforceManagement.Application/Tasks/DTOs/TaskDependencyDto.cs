namespace StudentWorkforceManagement.Application.Tasks.DTOs;

public sealed record TaskDependencyDto(Guid Id, Guid TaskId, Guid DependsOnTaskId);
