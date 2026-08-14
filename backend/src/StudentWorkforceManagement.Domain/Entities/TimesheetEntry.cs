using StudentWorkforceManagement.Domain.Common;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TimesheetEntry : AuditableEntity, IHasConcurrencyToken
{
    public Guid TimesheetWeekId { get; set; }
    public TimesheetWeek? TimesheetWeek { get; set; }
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public DateOnly WorkDate { get; set; }
    public int Minutes { get; set; }
    public string? Note { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
