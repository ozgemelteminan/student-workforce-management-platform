using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class RecurringTask : Entity, IHasConcurrencyToken
{
    public Guid TemplateId { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public DateTimeOffset NextRunAt { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedById { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
