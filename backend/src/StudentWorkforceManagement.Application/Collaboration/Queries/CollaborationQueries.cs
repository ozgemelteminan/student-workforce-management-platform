using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Collaboration.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Collaboration.Queries;

public sealed record GetTaskNudgeEligibilityQuery(Guid TaskId, Guid RecipientStudentId) : IRequest<NudgeEligibilityDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record GetCurrentTimesheetWeekQuery : IRequest<TimesheetWeekDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record GetTimesheetWeeksQuery : PagedQuery, IRequest<PaginatedResult<TimesheetWeekDto>>, IAuthorizableRequest
{
    public Guid? StudentId { get; init; }
    public TimesheetStatus? Status { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetTemporaryUnavailabilityQuery : IRequest<IReadOnlyCollection<TemporaryUnavailabilityDto>>, IAuthorizableRequest
{
    public Guid? StudentId { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetMeetingsQuery : PagedQuery, IRequest<PaginatedResult<MeetingDto>>, IAuthorizableRequest
{
    public MeetingStatus? Status { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetMeetingQuery(Guid MeetingId) : IRequest<MeetingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetMeetingSlotRecommendationsQuery(Guid MeetingId) : IRequest<IReadOnlyCollection<MeetingSlotRecommendationDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class CollaborationQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IUtcClock clock)
    : IRequestHandler<GetTaskNudgeEligibilityQuery, NudgeEligibilityDto>,
      IRequestHandler<GetCurrentTimesheetWeekQuery, TimesheetWeekDto>,
      IRequestHandler<GetTimesheetWeeksQuery, PaginatedResult<TimesheetWeekDto>>,
      IRequestHandler<GetTemporaryUnavailabilityQuery, IReadOnlyCollection<TemporaryUnavailabilityDto>>,
      IRequestHandler<GetMeetingsQuery, PaginatedResult<MeetingDto>>,
      IRequestHandler<GetMeetingQuery, MeetingDto>,
      IRequestHandler<GetMeetingSlotRecommendationsQuery, IReadOnlyCollection<MeetingSlotRecommendationDto>>
{
    private static readonly TimeSpan NudgeCooldown = TimeSpan.FromHours(3);

    public async Task<NudgeEligibilityDto> Handle(GetTaskNudgeEligibilityQuery request, CancellationToken cancellationToken)
    {
        var senderStudentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can inspect nudge eligibility.");
        var activeCount = await dbContext.TaskAssignmentHistory.AsNoTracking()
            .CountAsync(entity => entity.TaskId == request.TaskId && entity.IsActive && (entity.StudentId == senderStudentId || entity.StudentId == request.RecipientStudentId), cancellationToken);
        if (senderStudentId == request.RecipientStudentId || activeCount < 2)
        {
            return new NudgeEligibilityDto(false, null);
        }
        var lastSentAt = await dbContext.TaskNudges.AsNoTracking()
            .Where(entity => entity.TaskId == request.TaskId && entity.SenderStudentId == senderStudentId && entity.RecipientStudentId == request.RecipientStudentId)
            .OrderByDescending(entity => entity.SentAt)
            .Select(entity => (DateTimeOffset?)entity.SentAt)
            .FirstOrDefaultAsync(cancellationToken);
        var nextAllowedAt = lastSentAt?.Add(NudgeCooldown);
        return new NudgeEligibilityDto(!nextAllowedAt.HasValue || nextAllowedAt <= clock.UtcNow, nextAllowedAt);
    }

    public async Task<TimesheetWeekDto> Handle(GetCurrentTimesheetWeekQuery request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can view their current timesheet.");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, Istanbul()).DateTime);
        var weekStart = WeekStart(today);
        var week = await dbContext.TimesheetWeeks.SingleOrDefaultAsync(entity => entity.StudentId == studentId && entity.WeekStartDate == weekStart, cancellationToken);
        if (week is null)
        {
            var weeklyTargetMinutes = await dbContext.Students.AsNoTracking()
                .Where(student => student.Id == studentId)
                .Select(student => student.WeeklyTargetMinutes)
                .SingleAsync(cancellationToken);
            week = new StudentWorkforceManagement.Domain.Entities.TimesheetWeek { Id = Guid.NewGuid(), StudentId = studentId, WeekStartDate = weekStart, WeekEndDate = weekStart.AddDays(6), TargetMinutes = weeklyTargetMinutes ?? 0 };
            dbContext.TimesheetWeeks.Add(week);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return await LoadWeekDtoAsync(week.Id, cancellationToken);
    }

    public async Task<PaginatedResult<TimesheetWeekDto>> Handle(GetTimesheetWeeksQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.TimesheetWeeks.AsNoTracking().AsQueryable();
        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            query = query.Where(entity => entity.StudentId == currentUser.StudentId);
        }
        else if (request.StudentId.HasValue)
        {
            query = query.Where(entity => entity.StudentId == request.StudentId.Value);
        }
        if (request.Status.HasValue) query = query.Where(entity => entity.Status == request.Status.Value);
        return await query.OrderByDescending(entity => entity.WeekStartDate).Select(WeekProjection()).ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TemporaryUnavailabilityDto>> Handle(GetTemporaryUnavailabilityQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.TemporaryUnavailability.AsNoTracking().AsQueryable();
        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            query = query.Where(entity => entity.StudentId == currentUser.StudentId);
        }
        else if (request.StudentId.HasValue)
        {
            query = query.Where(entity => entity.StudentId == request.StudentId.Value);
        }
        return await query.OrderBy(entity => entity.StartAt).Select(entity => new TemporaryUnavailabilityDto(entity.Id, entity.StudentId, entity.StartAt, entity.EndAt, entity.Category, entity.Note, entity.ConcurrencyToken)).ToListAsync(cancellationToken);
    }

    public async Task<PaginatedResult<MeetingDto>> Handle(GetMeetingsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Meetings.AsNoTracking().AsQueryable();
        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            query = query.Where(meeting => meeting.Participants.Any(participant => participant.StudentId == currentUser.StudentId));
        }
        if (request.Status.HasValue) query = query.Where(meeting => meeting.Status == request.Status.Value);
        return await query.OrderByDescending(meeting => meeting.CreatedAt).Select(MeetingProjection()).ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async Task<MeetingDto> Handle(GetMeetingQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Meetings.AsNoTracking().Where(meeting => meeting.Id == request.MeetingId);
        if (currentUser.IsInRole(UserRole.STUDENT))
        {
            query = query.Where(meeting => meeting.Participants.Any(participant => participant.StudentId == currentUser.StudentId));
        }
        return await query.Select(MeetingProjection()).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Meeting", request.MeetingId);
    }

    public async Task<IReadOnlyCollection<MeetingSlotRecommendationDto>> Handle(GetMeetingSlotRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var meeting = await dbContext.Meetings.AsNoTracking().Where(entity => entity.Id == request.MeetingId).Select(MeetingProjection()).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Meeting", request.MeetingId);
        var ranges = meeting.Participants
            .Select(participant => new { participant.CampusPresence, Ranges = ParseRanges(participant.AvailableRangesJson) })
            .ToList();
        var candidates = ranges.SelectMany(item => item.Ranges).Select(range => range.StartAt).Distinct().OrderBy(value => value).Take(40);
        var results = new List<MeetingSlotRecommendationDto>();
        foreach (var start in candidates)
        {
            var end = start.AddHours(1);
            var available = ranges.Count(item => item.Ranges.Any(range => range.StartAt <= start && range.EndAt >= end));
            if (available == 0) continue;
            var onCampus = ranges.Count(item => item.CampusPresence == CampusPresence.ON_CAMPUS && item.Ranges.Any(range => range.StartAt <= start && range.EndAt >= end));
            results.Add(new MeetingSlotRecommendationDto(start, end, available, meeting.Participants.Count, onCampus));
        }
        return results.OrderByDescending(item => item.AvailableCount).ThenByDescending(item => item.OnCampusCount).ThenBy(item => item.StartAt).Take(10).ToArray();
    }

    private static DateOnly WeekStart(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static TimeZoneInfo Istanbul() => TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    private async Task<TimesheetWeekDto> LoadWeekDtoAsync(Guid weekId, CancellationToken cancellationToken)
    {
        return await dbContext.TimesheetWeeks.AsNoTracking().Where(week => week.Id == weekId).Select(WeekProjection()).SingleAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<StudentWorkforceManagement.Domain.Entities.TimesheetWeek, TimesheetWeekDto>> WeekProjection() =>
        week => new TimesheetWeekDto(week.Id, week.StudentId, week.WeekStartDate, week.WeekEndDate, week.TargetMinutes, week.Status, week.Entries.Sum(entry => entry.Minutes), week.SubmittedAt, week.ReviewedAt, week.ReviewedByUserId, week.ReviewerComment, week.ConcurrencyToken, week.Entries.OrderBy(entry => entry.WorkDate).Select(entry => new TimesheetEntryDto(entry.Id, entry.TimesheetWeekId, entry.TaskId, entry.WorkDate, entry.Minutes, entry.Note, entry.ConcurrencyToken, entry.Task != null ? entry.Task.Title : null)).ToList());

    private static System.Linq.Expressions.Expression<Func<StudentWorkforceManagement.Domain.Entities.Meeting, MeetingDto>> MeetingProjection() =>
        meeting => new MeetingDto(meeting.Id, meeting.Title, meeting.Type, meeting.Status, meeting.CreatedByUserId, meeting.ResponseDeadline, meeting.ConfirmedStartAt, meeting.ConfirmedEndAt, meeting.Location, meeting.Agenda, meeting.Notes, meeting.ConcurrencyToken, meeting.Participants.OrderBy(participant => participant.StudentId).Select(participant => new MeetingParticipantDto(participant.Id, participant.MeetingId, participant.StudentId, participant.CampusPresence, participant.AvailableRangesJson, participant.Note, participant.RespondedAt, participant.ConcurrencyToken)).ToList(), meeting.ActionItems.OrderBy(item => item.CreatedAt).Select(item => new MeetingActionItemDto(item.Id, item.MeetingId, item.Title, item.AssignedStudentId, item.TaskId, item.IsCompleted, item.ConcurrencyToken)).ToList());

    private static IReadOnlyCollection<RangeDto> ParseRanges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<RangeDto>();
        try
        {
            return JsonSerializer.Deserialize<List<RangeDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RangeDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<RangeDto>();
        }
    }

    private sealed record RangeDto(DateTimeOffset StartAt, DateTimeOffset EndAt);
}
