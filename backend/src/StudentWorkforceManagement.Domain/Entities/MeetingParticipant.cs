using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class MeetingParticipant : AuditableEntity, IHasConcurrencyToken
{
    public Guid MeetingId { get; set; }
    public Meeting? Meeting { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public CampusPresence? CampusPresence { get; set; }
    public string? AvailableRangesJson { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
