using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Collaboration.DTOs;

public sealed record TaskNudgeDto(Guid Id, Guid TaskId, Guid SenderStudentId, Guid RecipientStudentId, DateTimeOffset SentAt, DateTimeOffset NextAllowedAt);
public sealed record NudgeEligibilityDto(bool CanSend, DateTimeOffset? NextAllowedAt);
public sealed record TimesheetEntryDto(Guid Id, Guid TimesheetWeekId, Guid TaskId, DateOnly WorkDate, int Minutes, string? Note, Guid ConcurrencyToken, string? TaskTitle = null);
public sealed record TimesheetWeekDto(Guid Id, Guid StudentId, DateOnly WeekStartDate, DateOnly WeekEndDate, int TargetMinutes, TimesheetStatus Status, int TotalMinutes, DateTimeOffset? SubmittedAt, DateTimeOffset? ReviewedAt, Guid? ReviewedByUserId, string? ReviewerComment, Guid ConcurrencyToken, IReadOnlyCollection<TimesheetEntryDto> Entries);
public sealed record TemporaryUnavailabilityDto(Guid Id, Guid StudentId, DateTimeOffset StartAt, DateTimeOffset EndAt, string Category, string? Note, Guid ConcurrencyToken);
public sealed record MeetingParticipantDto(Guid Id, Guid MeetingId, Guid StudentId, CampusPresence? CampusPresence, string? AvailableRangesJson, string? Note, DateTimeOffset? RespondedAt, Guid ConcurrencyToken);
public sealed record MeetingActionItemDto(Guid Id, Guid MeetingId, string Title, Guid? AssignedStudentId, Guid? TaskId, bool IsCompleted, Guid ConcurrencyToken);
public sealed record MeetingDto(Guid Id, string Title, MeetingType Type, MeetingStatus Status, Guid CreatedByUserId, DateTimeOffset ResponseDeadline, DateTimeOffset? ConfirmedStartAt, DateTimeOffset? ConfirmedEndAt, string? Location, string? Agenda, string? Notes, Guid ConcurrencyToken, IReadOnlyCollection<MeetingParticipantDto> Participants, IReadOnlyCollection<MeetingActionItemDto> ActionItems);
public sealed record MeetingSlotRecommendationDto(DateTimeOffset StartAt, DateTimeOffset EndAt, int AvailableCount, int ParticipantCount, int OnCampusCount);
