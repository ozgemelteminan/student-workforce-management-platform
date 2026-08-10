using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class RecurringTask : AuditableEntity, IHasConcurrencyToken
{
    public Guid TemplateId { get; set; }
    public TaskTemplate? Template { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "Europe/Istanbul";
    public TimeOnly? LocalRunTime { get; set; }
    public DateTimeOffset NextRunAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public ICollection<RecurringTaskOccurrence> Occurrences { get; set; } = new List<RecurringTaskOccurrence>();
}
