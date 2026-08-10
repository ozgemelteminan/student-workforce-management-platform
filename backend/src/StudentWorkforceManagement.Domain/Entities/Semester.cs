using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Semester : AuditableEntity, IHasConcurrencyToken
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public SemesterStatus Status { get; set; }
    public Guid ConcurrencyToken { get; set; }

    public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
    public ICollection<Availability> Availability { get; set; } = new List<Availability>();
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
}
