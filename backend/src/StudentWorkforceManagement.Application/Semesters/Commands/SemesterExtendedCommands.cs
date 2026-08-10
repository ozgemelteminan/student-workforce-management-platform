using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Semesters.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Semesters.Commands;

public sealed record UpdateSemesterCommand(Guid SemesterId, string Name, DateOnly StartDate, DateOnly EndDate, SemesterStatus Status) : IRequest<SemesterDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}
public sealed record ArchiveSemesterCommand(Guid SemesterId) : IRequest<SemesterDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}
public sealed record DeleteSemesterCommand(Guid SemesterId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed class UpdateSemesterCommandValidator : AbstractValidator<UpdateSemesterCommand>
{
    public UpdateSemesterCommandValidator()
    {
        RuleFor(command => command.SemesterId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command).Must(command => command.EndDate >= command.StartDate).WithMessage("Semester end date must be after start date.");
    }
}

public sealed class SemesterExtendedCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateSemesterCommand, SemesterDto>, IRequestHandler<ArchiveSemesterCommand, SemesterDto>, IRequestHandler<DeleteSemesterCommand, Unit>
{
    public async System.Threading.Tasks.Task<SemesterDto> Handle(UpdateSemesterCommand request, CancellationToken cancellationToken)
    {
        var semester = await dbContext.Semesters.SingleOrDefaultAsync(entity => entity.Id == request.SemesterId, cancellationToken)
            ?? throw new NotFoundException("Semester", request.SemesterId);
        if (request.Status == SemesterStatus.ACTIVE && await dbContext.Semesters.AnyAsync(entity => entity.Id != request.SemesterId && entity.Status == SemesterStatus.ACTIVE, cancellationToken))
        {
            throw new ConflictException("Another active semester already exists. Use ActivateSemester to archive the previous active semester explicitly.");
        }
        semester.Name = request.Name.Trim();
        semester.StartDate = request.StartDate;
        semester.EndDate = request.EndDate;
        semester.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(semester);
    }

    public async System.Threading.Tasks.Task<SemesterDto> Handle(ArchiveSemesterCommand request, CancellationToken cancellationToken)
    {
        var semester = await dbContext.Semesters.SingleOrDefaultAsync(entity => entity.Id == request.SemesterId, cancellationToken)
            ?? throw new NotFoundException("Semester", request.SemesterId);
        semester.Status = SemesterStatus.ARCHIVED;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(semester);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteSemesterCommand request, CancellationToken cancellationToken)
    {
        var semester = await dbContext.Semesters.SingleOrDefaultAsync(entity => entity.Id == request.SemesterId, cancellationToken)
            ?? throw new NotFoundException("Semester", request.SemesterId);
        var hasReferences = await dbContext.Tasks.AnyAsync(task => task.SemesterId == request.SemesterId, cancellationToken)
            || await dbContext.CourseSchedules.AnyAsync(schedule => schedule.SemesterId == request.SemesterId, cancellationToken)
            || await dbContext.Availability.AnyAsync(availability => availability.SemesterId == request.SemesterId, cancellationToken);
        if (hasReferences)
        {
            throw new ConflictException("Referenced semesters must be archived instead of deleted.");
        }
        dbContext.Semesters.Remove(semester);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private static SemesterDto ToDto(StudentWorkforceManagement.Domain.Entities.Semester semester) => new(semester.Id, semester.Name, semester.StartDate, semester.EndDate, semester.Status, semester.ConcurrencyToken);
}
