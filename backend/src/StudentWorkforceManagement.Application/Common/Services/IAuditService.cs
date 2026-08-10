namespace StudentWorkforceManagement.Application.Common.Services;

public interface IAuditService
{
    System.Threading.Tasks.Task RecordAsync(string action, string entityType, Guid? entityId, string? oldValue = null, string? newValue = null, CancellationToken cancellationToken = default);
}
