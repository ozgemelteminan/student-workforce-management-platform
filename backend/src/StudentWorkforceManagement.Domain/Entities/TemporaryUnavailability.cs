using StudentWorkforceManagement.Domain.Common;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TemporaryUnavailability : AuditableEntity, IHasConcurrencyToken
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
