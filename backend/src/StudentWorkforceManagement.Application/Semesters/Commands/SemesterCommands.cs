using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Behaviors;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Semesters.DTOs;
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

namespace StudentWorkforceManagement.Application.Semesters.Commands;

public sealed record CreateSemesterCommand(string Name, DateOnly StartDate, DateOnly EndDate, SemesterStatus Status) : IRequest<SemesterDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}
public sealed record ActivateSemesterCommand(Guid SemesterId) : IRequest<SemesterDto>, IAuthorizableRequest, ITransactionalRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed class CreateSemesterCommandValidator : AbstractValidator<CreateSemesterCommand>
{
    public CreateSemesterCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command).Must(command => command.EndDate >= command.StartDate).WithMessage("Semester end date must be after start date.");
        RuleFor(command => command.Status).IsInEnum();
    }
}

public sealed class SemesterCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateSemesterCommand, SemesterDto>, IRequestHandler<ActivateSemesterCommand, SemesterDto>
{
    public System.Threading.Tasks.Task<SemesterDto> Handle(CreateSemesterCommand request, CancellationToken cancellationToken)
    {
        if (request.Status == SemesterStatus.ACTIVE)
        {
            foreach (var active in dbContext.Semesters.Where(semester => semester.Status == SemesterStatus.ACTIVE))
            {
                active.Status = SemesterStatus.ARCHIVED;
            }
        }
        var semester = new Semester { Id = Guid.NewGuid(), Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate, Status = request.Status };
        dbContext.Semesters.Add(semester);
        return System.Threading.Tasks.Task.FromResult(ToDto(semester));
    }

    public async System.Threading.Tasks.Task<SemesterDto> Handle(ActivateSemesterCommand request, CancellationToken cancellationToken)
    {
        var semester = await dbContext.Semesters.SingleOrDefaultAsync(item => item.Id == request.SemesterId, cancellationToken)
            ?? throw new NotFoundException("Semester", request.SemesterId);
        foreach (var active in dbContext.Semesters.Where(item => item.Status == SemesterStatus.ACTIVE && item.Id != request.SemesterId))
        {
            active.Status = SemesterStatus.ARCHIVED;
        }
        semester.Status = SemesterStatus.ACTIVE;
        return ToDto(semester);
    }

    private static SemesterDto ToDto(Semester semester) => new(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.Status, semester.ConcurrencyToken);
}
