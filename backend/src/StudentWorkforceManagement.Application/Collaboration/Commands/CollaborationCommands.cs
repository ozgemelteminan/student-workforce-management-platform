using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Collaboration.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Services;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Tasks.Commands.CreateTask;
using StudentWorkforceManagement.Application.Tasks.DTOs;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Collaboration.Commands;

public sealed record SendTaskNudgeCommand(Guid TaskId, Guid RecipientStudentId) : IRequest<TaskNudgeDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record UpsertTimesheetEntryCommand(Guid? EntryId, Guid TaskId, DateOnly WorkDate, int Minutes, string? Note) : IRequest<TimesheetWeekDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record DeleteTimesheetEntryCommand(Guid EntryId) : IRequest<TimesheetWeekDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record SubmitTimesheetWeekCommand(Guid TimesheetWeekId) : IRequest<TimesheetWeekDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record ReviewTimesheetWeekCommand(Guid TimesheetWeekId, TimesheetStatus Status, string? ReviewerComment) : IRequest<TimesheetWeekDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record CreateTemporaryUnavailabilityCommand(DateTimeOffset StartAt, DateTimeOffset EndAt, string Category, string? Note) : IRequest<TemporaryUnavailabilityDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record DeleteTemporaryUnavailabilityCommand(Guid Id) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record CreateMeetingCommand(string Title, MeetingType Type, DateTimeOffset ResponseDeadline, IReadOnlyCollection<Guid> ParticipantStudentIds, string? Location, string? Agenda) : IRequest<MeetingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record RespondToMeetingCommand(Guid MeetingId, CampusPresence CampusPresence, string AvailableRangesJson, string? Note) : IRequest<MeetingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed record ConfirmMeetingCommand(Guid MeetingId, DateTimeOffset StartAt, DateTimeOffset EndAt, string? Location) : IRequest<MeetingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record UpdateMeetingNotesCommand(Guid MeetingId, string? Title, string? Agenda, string? Notes) : IRequest<MeetingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record CancelMeetingCommand(Guid MeetingId) : IRequest<MeetingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record AddMeetingActionItemCommand(Guid MeetingId, string Title, Guid? AssignedStudentId) : IRequest<MeetingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record ConvertMeetingActionItemToTaskCommand(Guid MeetingId, Guid ActionItemId, Guid CategoryId, Guid? SemesterId, TaskPriority Priority, TaskDifficulty Difficulty, DateTimeOffset Deadline, int EstimatedDurationMinutes) : IRequest<TaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class CollaborationCommandValidator :
    AbstractValidator<UpsertTimesheetEntryCommand>
{
    public CollaborationCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.Minutes).GreaterThan(0);
        RuleFor(command => command.Note).MaximumLength(1000);
    }
}

public sealed class CollaborationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    INotificationIntentService notifications,
    IUtcClock clock,
    IRequestHandler<CreateTaskCommand, TaskDto> createTaskHandler)
    : IRequestHandler<SendTaskNudgeCommand, TaskNudgeDto>,
      IRequestHandler<UpsertTimesheetEntryCommand, TimesheetWeekDto>,
      IRequestHandler<DeleteTimesheetEntryCommand, TimesheetWeekDto>,
      IRequestHandler<SubmitTimesheetWeekCommand, TimesheetWeekDto>,
      IRequestHandler<ReviewTimesheetWeekCommand, TimesheetWeekDto>,
      IRequestHandler<CreateTemporaryUnavailabilityCommand, TemporaryUnavailabilityDto>,
      IRequestHandler<DeleteTemporaryUnavailabilityCommand, Unit>,
      IRequestHandler<CreateMeetingCommand, MeetingDto>,
      IRequestHandler<RespondToMeetingCommand, MeetingDto>,
      IRequestHandler<ConfirmMeetingCommand, MeetingDto>,
      IRequestHandler<UpdateMeetingNotesCommand, MeetingDto>,
      IRequestHandler<CancelMeetingCommand, MeetingDto>,
      IRequestHandler<AddMeetingActionItemCommand, MeetingDto>,
      IRequestHandler<ConvertMeetingActionItemToTaskCommand, TaskDto>
{
    private static readonly TimeSpan NudgeCooldown = TimeSpan.FromHours(3);

    public async System.Threading.Tasks.Task<TaskNudgeDto> Handle(SendTaskNudgeCommand request, CancellationToken cancellationToken)
    {
        var senderStudentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can send co-assignee nudges.");
        if (senderStudentId == request.RecipientStudentId)
        {
            throw new ConflictException("Students cannot nudge themselves.");
        }

        var assignments = await dbContext.TaskAssignmentHistory.AsNoTracking()
            .Where(entity => entity.TaskId == request.TaskId && entity.IsActive && (entity.StudentId == senderStudentId || entity.StudentId == request.RecipientStudentId))
            .Select(entity => entity.StudentId)
            .ToListAsync(cancellationToken);
        if (!assignments.Contains(senderStudentId) || !assignments.Contains(request.RecipientStudentId))
        {
            throw new ForbiddenException("Sender and recipient must both be active assignees of the task.");
        }

        var cutoff = clock.UtcNow.Subtract(NudgeCooldown);
        var last = await dbContext.TaskNudges.AsNoTracking()
            .Where(entity => entity.TaskId == request.TaskId && entity.SenderStudentId == senderStudentId && entity.RecipientStudentId == request.RecipientStudentId)
            .OrderByDescending(entity => entity.SentAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (last is not null && last.SentAt > cutoff)
        {
            throw new ConflictException($"Nudge cooldown is active until {last.SentAt.Add(NudgeCooldown):O}.");
        }

        var taskTitle = await dbContext.Tasks.AsNoTracking().Where(entity => entity.Id == request.TaskId).Select(entity => entity.Title).SingleAsync(cancellationToken);
        var recipientUserId = await dbContext.Students.AsNoTracking().Where(entity => entity.Id == request.RecipientStudentId).Select(entity => entity.UserId).SingleAsync(cancellationToken);
        var senderName = await dbContext.Students.AsNoTracking().Where(entity => entity.Id == senderStudentId).Select(entity => (entity.FirstName + " " + entity.LastName).Trim()).SingleAsync(cancellationToken);
        var nudge = new TaskNudge { Id = Guid.NewGuid(), TaskId = request.TaskId, SenderStudentId = senderStudentId, RecipientStudentId = request.RecipientStudentId, SentAt = clock.UtcNow };
        dbContext.TaskNudges.Add(nudge);
        await notifications.CreateAsync(recipientUserId, NotificationType.NUDGE, "Task nudge", $"{senderName} nudged you about \"{taskTitle}\".", "Task", request.TaskId, $"nudge:{request.TaskId}:{senderStudentId}:{request.RecipientStudentId}:{nudge.SentAt:yyyyMMddHH}", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new TaskNudgeDto(nudge.Id, nudge.TaskId, nudge.SenderStudentId, nudge.RecipientStudentId, nudge.SentAt, nudge.SentAt.Add(NudgeCooldown));
    }

    public async System.Threading.Tasks.Task<TimesheetWeekDto> Handle(UpsertTimesheetEntryCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can edit their timesheet.");
        var weekStart = WeekStart(request.WorkDate);
        var week = await GetOrCreateWeekAsync(studentId, weekStart, cancellationToken);
        EnsureEditable(week);
        if (!await dbContext.TaskAssignmentHistory.AnyAsync(entity => entity.TaskId == request.TaskId && entity.StudentId == studentId && entity.IsActive, cancellationToken))
        {
            throw new ForbiddenException("Students can record time only for active assigned tasks.");
        }

        TimesheetEntry entry;
        if (request.EntryId.HasValue)
        {
            entry = await dbContext.TimesheetEntries.Include(entity => entity.TimesheetWeek).SingleOrDefaultAsync(entity => entity.Id == request.EntryId.Value && entity.TimesheetWeek!.StudentId == studentId, cancellationToken)
                ?? throw new NotFoundException("TimesheetEntry", request.EntryId.Value);
            EnsureEditable(entry.TimesheetWeek!);
            entry.TaskId = request.TaskId;
            entry.WorkDate = request.WorkDate;
            entry.Minutes = request.Minutes;
            entry.Note = request.Note;
        }
        else
        {
            entry = new TimesheetEntry { Id = Guid.NewGuid(), TimesheetWeekId = week.Id, TaskId = request.TaskId, WorkDate = request.WorkDate, Minutes = request.Minutes, Note = request.Note };
            dbContext.TimesheetEntries.Add(entry);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadWeekDtoAsync(week.Id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<TimesheetWeekDto> Handle(DeleteTimesheetEntryCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can edit their timesheet.");
        var entry = await dbContext.TimesheetEntries.Include(entity => entity.TimesheetWeek).SingleOrDefaultAsync(entity => entity.Id == request.EntryId && entity.TimesheetWeek!.StudentId == studentId, cancellationToken)
            ?? throw new NotFoundException("TimesheetEntry", request.EntryId);
        EnsureEditable(entry.TimesheetWeek!);
        var weekId = entry.TimesheetWeekId;
        dbContext.TimesheetEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadWeekDtoAsync(weekId, cancellationToken);
    }

    public async System.Threading.Tasks.Task<TimesheetWeekDto> Handle(SubmitTimesheetWeekCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can submit their timesheet.");
        var week = await dbContext.TimesheetWeeks.SingleOrDefaultAsync(entity => entity.Id == request.TimesheetWeekId && entity.StudentId == studentId, cancellationToken)
            ?? throw new NotFoundException("TimesheetWeek", request.TimesheetWeekId);
        if (week.Status is not (TimesheetStatus.DRAFT or TimesheetStatus.NEEDS_CORRECTION))
        {
            throw new ConflictException("Only draft or correction-needed weeks can be submitted.");
        }
        week.Status = TimesheetStatus.SUBMITTED;
        week.SubmittedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadWeekDtoAsync(week.Id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<TimesheetWeekDto> Handle(ReviewTimesheetWeekCommand request, CancellationToken cancellationToken)
    {
        if (request.Status is not (TimesheetStatus.APPROVED or TimesheetStatus.NEEDS_CORRECTION))
        {
            throw new ConflictException("Timesheet review must approve or request correction.");
        }
        var week = await dbContext.TimesheetWeeks.SingleOrDefaultAsync(entity => entity.Id == request.TimesheetWeekId, cancellationToken)
            ?? throw new NotFoundException("TimesheetWeek", request.TimesheetWeekId);
        if (week.Status != TimesheetStatus.SUBMITTED)
        {
            throw new ConflictException("Only submitted weeks can be reviewed.");
        }
        week.Status = request.Status;
        week.ReviewedAt = clock.UtcNow;
        week.ReviewedByUserId = currentUser.RequireUserId();
        week.ReviewerComment = request.ReviewerComment;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadWeekDtoAsync(week.Id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<TemporaryUnavailabilityDto> Handle(CreateTemporaryUnavailabilityCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can create temporary unavailability.");
        if (request.EndAt <= request.StartAt)
        {
            throw new ConflictException("Temporary unavailability end must be after start.");
        }
        var entity = new TemporaryUnavailability { Id = Guid.NewGuid(), StudentId = studentId, StartAt = request.StartAt.ToUniversalTime(), EndAt = request.EndAt.ToUniversalTime(), Category = request.Category.Trim(), Note = request.Note };
        dbContext.TemporaryUnavailability.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteTemporaryUnavailabilityCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can delete temporary unavailability.");
        var entity = await dbContext.TemporaryUnavailability.SingleOrDefaultAsync(item => item.Id == request.Id && item.StudentId == studentId, cancellationToken)
            ?? throw new NotFoundException("TemporaryUnavailability", request.Id);
        dbContext.TemporaryUnavailability.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async System.Threading.Tasks.Task<MeetingDto> Handle(CreateMeetingCommand request, CancellationToken cancellationToken)
    {
        var uniqueParticipants = request.ParticipantStudentIds.Distinct().ToArray();
        if (uniqueParticipants.Length == 0)
        {
            throw new ConflictException("Meetings require at least one participant.");
        }
        var existingCount = await dbContext.Students.CountAsync(student => uniqueParticipants.Contains(student.Id) && student.IsActive, cancellationToken);
        if (existingCount != uniqueParticipants.Length)
        {
            throw new NotFoundException("Student", "one or more participants");
        }
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = request.Title.Trim(), Type = request.Type, Status = MeetingStatus.AVAILABILITY_REQUESTED, CreatedByUserId = currentUser.RequireUserId(), ResponseDeadline = request.ResponseDeadline.ToUniversalTime(), Location = request.Location, Agenda = request.Agenda };
        foreach (var participantId in uniqueParticipants)
        {
            meeting.Participants.Add(new MeetingParticipant { Id = Guid.NewGuid(), MeetingId = meeting.Id, StudentId = participantId });
        }
        dbContext.Meetings.Add(meeting);
        await NotifyMeetingParticipantsAsync(meeting, "Meeting availability requested", "Please submit your availability for this meeting.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadMeetingDtoAsync(meeting.Id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<MeetingDto> Handle(RespondToMeetingCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId ?? throw new ForbiddenException("Only students can respond to meetings.");
        var participant = await dbContext.MeetingParticipants.Include(entity => entity.Meeting).SingleOrDefaultAsync(entity => entity.MeetingId == request.MeetingId && entity.StudentId == studentId, cancellationToken)
            ?? throw new ForbiddenException("Only meeting participants can submit availability.");
        if (participant.Meeting!.Status == MeetingStatus.CANCELLED)
        {
            throw new ConflictException("Cancelled meetings cannot accept responses.");
        }
        participant.CampusPresence = request.CampusPresence;
        participant.AvailableRangesJson = request.AvailableRangesJson;
        participant.Note = request.Note;
        participant.RespondedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadMeetingDtoAsync(request.MeetingId, cancellationToken);
    }

    public async System.Threading.Tasks.Task<MeetingDto> Handle(ConfirmMeetingCommand request, CancellationToken cancellationToken)
    {
        if (request.EndAt <= request.StartAt)
        {
            throw new ConflictException("Meeting end must be after start.");
        }
        var meeting = await dbContext.Meetings.SingleOrDefaultAsync(entity => entity.Id == request.MeetingId, cancellationToken)
            ?? throw new NotFoundException("Meeting", request.MeetingId);
        meeting.Status = MeetingStatus.CONFIRMED;
        meeting.ConfirmedStartAt = request.StartAt.ToUniversalTime();
        meeting.ConfirmedEndAt = request.EndAt.ToUniversalTime();
        meeting.Location = request.Location;
        await NotifyMeetingParticipantsAsync(meeting, "Meeting confirmed", "A meeting time has been confirmed.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadMeetingDtoAsync(meeting.Id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<MeetingDto> Handle(UpdateMeetingNotesCommand request, CancellationToken cancellationToken)
    {
        var meeting = await dbContext.Meetings.SingleOrDefaultAsync(entity => entity.Id == request.MeetingId, cancellationToken)
            ?? throw new NotFoundException("Meeting", request.MeetingId);
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            meeting.Title = request.Title.Trim();
        }
        meeting.Agenda = request.Agenda;
        meeting.Notes = request.Notes;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadMeetingDtoAsync(meeting.Id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<MeetingDto> Handle(CancelMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await dbContext.Meetings.SingleOrDefaultAsync(entity => entity.Id == request.MeetingId, cancellationToken)
            ?? throw new NotFoundException("Meeting", request.MeetingId);
        meeting.Status = MeetingStatus.CANCELLED;
        meeting.CancelledAt = clock.UtcNow;
        await NotifyMeetingParticipantsAsync(meeting, "Meeting cancelled", "A meeting has been cancelled.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadMeetingDtoAsync(meeting.Id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<MeetingDto> Handle(AddMeetingActionItemCommand request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Meetings.AnyAsync(entity => entity.Id == request.MeetingId, cancellationToken))
        {
            throw new NotFoundException("Meeting", request.MeetingId);
        }
        dbContext.MeetingActionItems.Add(new MeetingActionItem { Id = Guid.NewGuid(), MeetingId = request.MeetingId, Title = request.Title.Trim(), AssignedStudentId = request.AssignedStudentId });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadMeetingDtoAsync(request.MeetingId, cancellationToken);
    }

    public async System.Threading.Tasks.Task<TaskDto> Handle(ConvertMeetingActionItemToTaskCommand request, CancellationToken cancellationToken)
    {
        var actionItem = await dbContext.MeetingActionItems.SingleOrDefaultAsync(entity => entity.Id == request.ActionItemId && entity.MeetingId == request.MeetingId, cancellationToken)
            ?? throw new NotFoundException("MeetingActionItem", request.ActionItemId);
        if (actionItem.TaskId.HasValue)
        {
            throw new ConflictException("Action item has already been converted to a task.");
        }
        var task = await createTaskHandler.Handle(new CreateTaskCommand(actionItem.Title, null, request.CategoryId, request.SemesterId, request.Priority, request.Difficulty, null, request.Deadline, request.EstimatedDurationMinutes), cancellationToken);
        actionItem.TaskId = task.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        return task;
    }

    private async System.Threading.Tasks.Task<TimesheetWeek> GetOrCreateWeekAsync(Guid studentId, DateOnly weekStart, CancellationToken cancellationToken)
    {
        var week = await dbContext.TimesheetWeeks.SingleOrDefaultAsync(entity => entity.StudentId == studentId && entity.WeekStartDate == weekStart, cancellationToken);
        if (week is not null) return week;
        var weeklyTargetMinutes = await dbContext.Students.AsNoTracking()
            .Where(student => student.Id == studentId)
            .Select(student => student.WeeklyTargetMinutes)
            .SingleAsync(cancellationToken);
        week = new TimesheetWeek { Id = Guid.NewGuid(), StudentId = studentId, WeekStartDate = weekStart, WeekEndDate = weekStart.AddDays(6), TargetMinutes = weeklyTargetMinutes ?? 0 };
        dbContext.TimesheetWeeks.Add(week);
        return week;
    }

    private static DateOnly WeekStart(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static void EnsureEditable(TimesheetWeek week)
    {
        if (week.Status is not (TimesheetStatus.DRAFT or TimesheetStatus.NEEDS_CORRECTION))
        {
            throw new ConflictException("Timesheet entries can be changed only while the week is draft or needs correction.");
        }
    }

    private async System.Threading.Tasks.Task<TimesheetWeekDto> LoadWeekDtoAsync(Guid weekId, CancellationToken cancellationToken)
    {
        return await dbContext.TimesheetWeeks.AsNoTracking()
            .Where(week => week.Id == weekId)
            .Select(week => new TimesheetWeekDto(
                week.Id, week.StudentId, week.WeekStartDate, week.WeekEndDate, week.TargetMinutes, week.Status, week.Entries.Sum(entry => entry.Minutes),
                week.SubmittedAt, week.ReviewedAt, week.ReviewedByUserId, week.ReviewerComment, week.ConcurrencyToken,
                week.Entries.OrderBy(entry => entry.WorkDate).Select(entry => new TimesheetEntryDto(entry.Id, entry.TimesheetWeekId, entry.TaskId, entry.WorkDate, entry.Minutes, entry.Note, entry.ConcurrencyToken, entry.Task != null ? entry.Task.Title : null)).ToList()))
            .SingleAsync(cancellationToken);
    }

    private async System.Threading.Tasks.Task<MeetingDto> LoadMeetingDtoAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        return await dbContext.Meetings.AsNoTracking().Where(meeting => meeting.Id == meetingId).Select(meeting => new MeetingDto(
            meeting.Id, meeting.Title, meeting.Type, meeting.Status, meeting.CreatedByUserId, meeting.ResponseDeadline, meeting.ConfirmedStartAt, meeting.ConfirmedEndAt,
            meeting.Location, meeting.Agenda, meeting.Notes, meeting.ConcurrencyToken,
            meeting.Participants.OrderBy(participant => participant.StudentId).Select(participant => new MeetingParticipantDto(participant.Id, participant.MeetingId, participant.StudentId, participant.CampusPresence, participant.AvailableRangesJson, participant.Note, participant.RespondedAt, participant.ConcurrencyToken)).ToList(),
            meeting.ActionItems.OrderBy(item => item.CreatedAt).Select(item => new MeetingActionItemDto(item.Id, item.MeetingId, item.Title, item.AssignedStudentId, item.TaskId, item.IsCompleted, item.ConcurrencyToken)).ToList())).SingleAsync(cancellationToken);
    }

    private async System.Threading.Tasks.Task NotifyMeetingParticipantsAsync(Meeting meeting, string title, string message, CancellationToken cancellationToken)
    {
        var participantUserIds = await dbContext.Students.AsNoTracking().Where(student => meeting.Participants.Select(participant => participant.StudentId).Contains(student.Id)).Select(student => student.UserId).ToListAsync(cancellationToken);
        foreach (var userId in participantUserIds)
        {
            await notifications.CreateAsync(userId, NotificationType.MEETING, title, message, "Meeting", meeting.Id, $"{title}:{meeting.Id}:{userId}:{clock.UtcNow:yyyyMMddHHmm}", cancellationToken);
        }
    }

    private static TemporaryUnavailabilityDto ToDto(TemporaryUnavailability entity) => new(entity.Id, entity.StudentId, entity.StartAt, entity.EndAt, entity.Category, entity.Note, entity.ConcurrencyToken);
}
