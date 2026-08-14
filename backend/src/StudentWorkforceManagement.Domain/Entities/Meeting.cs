using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Meeting : AuditableEntity, IHasConcurrencyToken
{
    public string Title { get; set; } = string.Empty;
    public MeetingType Type { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.DRAFT;
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTimeOffset ResponseDeadline { get; set; }
    public DateTimeOffset? ConfirmedStartAt { get; set; }
    public DateTimeOffset? ConfirmedEndAt { get; set; }
    public string? Location { get; set; }
    public string? Agenda { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
    public ICollection<MeetingActionItem> ActionItems { get; set; } = new List<MeetingActionItem>();
}
