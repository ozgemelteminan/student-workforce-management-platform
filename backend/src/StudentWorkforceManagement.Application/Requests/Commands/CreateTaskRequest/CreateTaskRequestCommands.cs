using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Requests.DTOs;
using Announcement = StudentWorkforceManagement.Domain.Entities.Announcement;
using AuditLog = StudentWorkforceManagement.Domain.Entities.AuditLog;
using AvailabilityEntity = StudentWorkforceManagement.Domain.Entities.Availability;
using Category = StudentWorkforceManagement.Domain.Entities.Category;
using CourseSchedule = StudentWorkforceManagement.Domain.Entities.CourseSchedule;
using EmailDelivery = StudentWorkforceManagement.Domain.Entities.EmailDelivery;
using MarketplaceClaim = StudentWorkforceManagement.Domain.Entities.MarketplaceClaim;
using MarketplaceListing = StudentWorkforceManagement.Domain.Entities.MarketplaceListing;
using Notification = StudentWorkforceManagement.Domain.Entities.Notification;
using NotificationPreference = StudentWorkforceManagement.Domain.Entities.NotificationPreference;
using Semester = StudentWorkforceManagement.Domain.Entities.Semester;
using Skill = StudentWorkforceManagement.Domain.Entities.Skill;
using StudentSkill = StudentWorkforceManagement.Domain.Entities.StudentSkill;
using SubmissionVersion = StudentWorkforceManagement.Domain.Entities.SubmissionVersion;
using SystemSetting = StudentWorkforceManagement.Domain.Entities.SystemSetting;
using TaskAssignmentHistory = StudentWorkforceManagement.Domain.Entities.TaskAssignmentHistory;
using TaskChecklistItem = StudentWorkforceManagement.Domain.Entities.TaskChecklistItem;
using TaskComment = StudentWorkforceManagement.Domain.Entities.TaskComment;
using TaskDependency = StudentWorkforceManagement.Domain.Entities.TaskDependency;
using TaskRequest = StudentWorkforceManagement.Domain.Entities.TaskRequest;
using TaskReview = StudentWorkforceManagement.Domain.Entities.TaskReview;
using TaskSubmission = StudentWorkforceManagement.Domain.Entities.TaskSubmission;
using DepartmentFile = StudentWorkforceManagement.Domain.Entities.DepartmentFile;
using Feedback = StudentWorkforceManagement.Domain.Entities.Feedback;
using FileFolder = StudentWorkforceManagement.Domain.Entities.FileFolder;
using Invitation = StudentWorkforceManagement.Domain.Entities.Invitation;
using RecurringTask = StudentWorkforceManagement.Domain.Entities.RecurringTask;
using RefreshToken = StudentWorkforceManagement.Domain.Entities.RefreshToken;
using Role = StudentWorkforceManagement.Domain.Entities.Role;
using Session = StudentWorkforceManagement.Domain.Entities.Session;
using Student = StudentWorkforceManagement.Domain.Entities.Student;
using TaskRequiredSkill = StudentWorkforceManagement.Domain.Entities.TaskRequiredSkill;
using TaskTemplate = StudentWorkforceManagement.Domain.Entities.TaskTemplate;
using User = StudentWorkforceManagement.Domain.Entities.User;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Requests.Commands.CreateTaskRequest;

public sealed record CreateExtensionRequestCommand(Guid TaskId, DateTimeOffset RequestedDeadline, string Reason) : IRequest<TaskRequestDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}
public sealed record CreateReassignmentRequestCommand(Guid TaskId, string Reason, Guid? SuggestedStudentId = null) : IRequest<TaskRequestDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed class CreateExtensionRequestCommandValidator : AbstractValidator<CreateExtensionRequestCommand>
{
    public CreateExtensionRequestCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.RequestedDeadline).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class CreateReassignmentRequestCommandValidator : AbstractValidator<CreateReassignmentRequestCommand>
{
    public CreateReassignmentRequestCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class CreateTaskRequestCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<CreateExtensionRequestCommand, TaskRequestDto>, IRequestHandler<CreateReassignmentRequestCommand, TaskRequestDto>
{
    public async System.Threading.Tasks.Task<TaskRequestDto> Handle(CreateExtensionRequestCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.RequireStudentId();
        var task = await dbContext.Tasks.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
        if (task.AssignedStudentId != studentId)
        {
            throw new ForbiddenException("Students may request extensions only for their assigned tasks.");
        }
        if (request.RequestedDeadline <= task.Deadline)
        {
            throw new ConflictException("Requested deadline must be later than the current deadline.");
        }
        await EnsureNoPendingRequestAsync(request.TaskId, RequestType.EXTENSION, cancellationToken);

        var entity = new TaskRequest
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            RequestedById = studentId,
            Type = RequestType.EXTENSION,
            Reason = request.Reason.Trim(),
            CurrentDeadline = task.Deadline,
            RequestedDeadline = request.RequestedDeadline.ToUniversalTime(),
            Status = RequestStatus.PENDING
        };
        dbContext.TaskRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RequestMappings.ToDto(entity);
    }

    public async System.Threading.Tasks.Task<TaskRequestDto> Handle(CreateReassignmentRequestCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.RequireStudentId();
        var task = await dbContext.Tasks.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
        if (task.AssignedStudentId != studentId)
        {
            throw new ForbiddenException("Students may request reassignment only for their assigned tasks.");
        }
        await EnsureNoPendingRequestAsync(request.TaskId, RequestType.REASSIGNMENT, cancellationToken);

        var entity = new TaskRequest
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            RequestedById = studentId,
            Type = RequestType.REASSIGNMENT,
            Reason = request.Reason.Trim(),
            SuggestedStudentId = request.SuggestedStudentId,
            Status = RequestStatus.PENDING
        };
        dbContext.TaskRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RequestMappings.ToDto(entity);
    }

    private async Task EnsureNoPendingRequestAsync(Guid taskId, RequestType type, CancellationToken cancellationToken)
    {
        if (await dbContext.TaskRequests.AnyAsync(entity => entity.TaskId == taskId && entity.Type == type && entity.Status == RequestStatus.PENDING, cancellationToken))
        {
            throw new ConflictException($"A pending {type} request already exists for this task.");
        }
    }
}
