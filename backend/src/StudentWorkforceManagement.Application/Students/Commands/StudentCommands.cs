using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Students.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Students.Commands;

public sealed record UpdateStudentProfileCommand(Guid StudentId, string FirstName, string LastName, string Email, string Department, int? WeeklyTargetMinutes) : IRequest<StudentDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record ActivateStudentCommand(Guid StudentId) : IRequest<StudentDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record DeactivateStudentCommand(Guid StudentId) : IRequest<StudentDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed class UpdateStudentProfileCommandValidator : AbstractValidator<UpdateStudentProfileCommand>
{
    public UpdateStudentProfileCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Department).NotEmpty().MaximumLength(160);
        RuleFor(command => command.WeeklyTargetMinutes).GreaterThanOrEqualTo(0).When(command => command.WeeklyTargetMinutes.HasValue);
    }
}

public sealed class StudentCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<UpdateStudentProfileCommand, StudentDto>, IRequestHandler<ActivateStudentCommand, StudentDto>, IRequestHandler<DeactivateStudentCommand, StudentDto>
{
    public async System.Threading.Tasks.Task<StudentDto> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new ForbiddenException("Students may update only their own profile.");
        }
        if (!currentUser.IsInRole(UserRole.STUDENT) && !currentUser.IsInRole(UserRole.ADMIN))
        {
            throw new ForbiddenException("Only admins or the student owner may update a student profile.");
        }
        var student = await dbContext.Students.SingleOrDefaultAsync(entity => entity.Id == request.StudentId, cancellationToken)
            ?? throw new NotFoundException("Student", request.StudentId);
        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();
        student.Email = request.Email.Trim().ToLowerInvariant();
        student.Department = request.Department.Trim();
        if (currentUser.IsInRole(UserRole.ADMIN))
        {
            student.WeeklyTargetMinutes = request.WeeklyTargetMinutes;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(student);
    }

    public System.Threading.Tasks.Task<StudentDto> Handle(ActivateStudentCommand request, CancellationToken cancellationToken) => SetActiveAsync(request.StudentId, true, cancellationToken);
    public System.Threading.Tasks.Task<StudentDto> Handle(DeactivateStudentCommand request, CancellationToken cancellationToken) => SetActiveAsync(request.StudentId, false, cancellationToken);

    private async System.Threading.Tasks.Task<StudentDto> SetActiveAsync(Guid studentId, bool isActive, CancellationToken cancellationToken)
    {
        var student = await dbContext.Students.SingleOrDefaultAsync(entity => entity.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student", studentId);
        student.IsActive = isActive;
        if (student.UserId != Guid.Empty)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == student.UserId, cancellationToken);
            if (user is not null)
            {
                user.IsActive = isActive;
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(student);
    }

    private static StudentDto ToDto(StudentWorkforceManagement.Domain.Entities.Student student)
    {
        return new StudentDto(student.Id, student.UserId, student.FirstName, student.LastName, student.Email, student.Department, student.WeeklyTargetMinutes, student.IsActive, student.CreatedAt, student.UpdatedAt, student.ConcurrencyToken);
    }
}
