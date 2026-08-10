using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Availability.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Availability.Commands;

public sealed record UpdateAvailabilityCommand(Guid AvailabilityId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, AvailabilityStatus Status, string? Reason) : IRequest<AvailabilityDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record DeleteAvailabilityCommand(Guid AvailabilityId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class UpdateAvailabilityCommandValidator : AbstractValidator<UpdateAvailabilityCommand>
{
    public UpdateAvailabilityCommandValidator()
    {
        RuleFor(command => command.AvailabilityId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command).Must(command => command.EndTime > command.StartTime).WithMessage("Availability end time must be after start time.");
    }
}

public sealed class AvailabilityCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<UpdateAvailabilityCommand, AvailabilityDto>, IRequestHandler<DeleteAvailabilityCommand, Unit>
{
    public async System.Threading.Tasks.Task<AvailabilityDto> Handle(UpdateAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var availability = await dbContext.Availability.SingleOrDefaultAsync(entity => entity.Id == request.AvailabilityId, cancellationToken)
            ?? throw new NotFoundException("Availability", request.AvailabilityId);
        EnsureCanModify(availability.StudentId);
        var overlaps = await dbContext.Availability.AnyAsync(item => item.Id != availability.Id && item.StudentId == availability.StudentId && item.SemesterId == availability.SemesterId && item.DayOfWeek == request.DayOfWeek && item.StartTime < request.EndTime && request.StartTime < item.EndTime, cancellationToken);
        if (overlaps)
        {
            throw new ConflictException("Availability overlaps an existing availability record.");
        }
        availability.DayOfWeek = request.DayOfWeek;
        availability.StartTime = request.StartTime;
        availability.EndTime = request.EndTime;
        availability.Status = request.Status;
        availability.Reason = request.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(availability);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var availability = await dbContext.Availability.SingleOrDefaultAsync(entity => entity.Id == request.AvailabilityId, cancellationToken)
            ?? throw new NotFoundException("Availability", request.AvailabilityId);
        EnsureCanModify(availability.StudentId);
        dbContext.Availability.Remove(availability);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private void EnsureCanModify(Guid studentId)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != studentId)
        {
            throw new ForbiddenException("Students may edit only their own availability.");
        }
        if (!currentUser.IsInRole(UserRole.STUDENT) && !currentUser.IsInRole(UserRole.ADMIN) && !currentUser.IsInRole(UserRole.TASK_MANAGER))
        {
            throw new ForbiddenException("Only students and authorized staff may modify availability.");
        }
    }

    private static AvailabilityDto ToDto(StudentWorkforceManagement.Domain.Entities.Availability availability) => new(availability.Id, availability.StudentId, availability.SemesterId, availability.DayOfWeek, availability.StartTime, availability.EndTime, availability.Status, availability.Reason, availability.ConcurrencyToken);
}
