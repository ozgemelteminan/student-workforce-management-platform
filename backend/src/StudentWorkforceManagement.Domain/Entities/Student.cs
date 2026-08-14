using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Student : AuditableEntity, ISoftDeletable, IHasConcurrencyToken
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int? WeeklyTargetMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }

    public ICollection<StudentSkill> Skills { get; set; } = new List<StudentSkill>();
    public ICollection<CourseSchedule> CourseSchedules { get; set; } = new List<CourseSchedule>();
    public ICollection<Availability> Availability { get; set; } = new List<Availability>();
    public ICollection<TaskAssignmentHistory> AssignmentHistory { get; set; } = new List<TaskAssignmentHistory>();
    public ICollection<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
    public ICollection<TaskRequest> Requests { get; set; } = new List<TaskRequest>();
    public ICollection<MarketplaceClaim> MarketplaceClaims { get; set; } = new List<MarketplaceClaim>();
    public ICollection<Feedback> FeedbackReceived { get; set; } = new List<Feedback>();
    public ICollection<TimesheetWeek> TimesheetWeeks { get; set; } = new List<TimesheetWeek>();
    public ICollection<TemporaryUnavailability> TemporaryUnavailability { get; set; } = new List<TemporaryUnavailability>();
    public ICollection<TaskNudge> SentNudges { get; set; } = new List<TaskNudge>();
    public ICollection<TaskNudge> ReceivedNudges { get; set; } = new List<TaskNudge>();
    public ICollection<MeetingParticipant> MeetingParticipants { get; set; } = new List<MeetingParticipant>();
    public ICollection<MeetingActionItem> MeetingActionItems { get; set; } = new List<MeetingActionItem>();
}
