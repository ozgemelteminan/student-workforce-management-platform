using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.Services;
using StudentWorkforceManagement.Application.Students.DTOs;
using StudentWorkforceManagement.Domain.Enums;
using TaskStatus = StudentWorkforceManagement.Domain.Enums.TaskStatus;

namespace StudentWorkforceManagement.Application.Students.Queries;

public sealed record GetStudentsQuery : PagedQuery, IRequest<PaginatedResult<StudentDto>>, IAuthorizableRequest
{
    public bool? IsActive { get; init; }
    public string? Department { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record GetStudentQuery(Guid StudentId) : IRequest<StudentProfileDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetCurrentStudentProfileQuery : IRequest<StudentProfileDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.Students;
}

public sealed class StudentQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, ICurrentUserService currentUser, ITaskWorkloadService workloadService)
    : IRequestHandler<GetStudentsQuery, PaginatedResult<StudentDto>>, IRequestHandler<GetStudentQuery, StudentProfileDto>, IRequestHandler<GetCurrentStudentProfileQuery, StudentProfileDto>
{
    public async System.Threading.Tasks.Task<PaginatedResult<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Students.AsNoTracking().AsQueryable();
        if (request.IsActive.HasValue)
        {
            query = query.Where(student => student.IsActive == request.IsActive.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var department = request.Department.Trim();
            query = query.Where(student => student.Department == department);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(student => student.FirstName.ToLower().Contains(term) || student.LastName.ToLower().Contains(term) || student.Email.ToLower().Contains(term) || student.Department.ToLower().Contains(term));
        }
        query = (request.SortBy?.ToLowerInvariant(), request.SortDirection?.ToLowerInvariant()) switch
        {
            ("name", "desc") => query.OrderByDescending(student => student.LastName).ThenByDescending(student => student.FirstName),
            ("email", "desc") => query.OrderByDescending(student => student.Email),
            ("department", "desc") => query.OrderByDescending(student => student.Department).ThenBy(student => student.LastName),
            ("created", "desc") => query.OrderByDescending(student => student.CreatedAt),
            ("email", _) => query.OrderBy(student => student.Email),
            ("department", _) => query.OrderBy(student => student.Department).ThenBy(student => student.LastName),
            ("created", _) => query.OrderBy(student => student.CreatedAt),
            _ => query.OrderBy(student => student.LastName).ThenBy(student => student.FirstName)
        };
        return await query.Select(ToDtoExpression()).ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public System.Threading.Tasks.Task<StudentProfileDto> Handle(GetStudentQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.STUDENT) && currentUser.StudentId != request.StudentId)
        {
            throw new ForbiddenException("Students may view only their own profile.");
        }
        return LoadProfileAsync(request.StudentId, cancellationToken);
    }

    public System.Threading.Tasks.Task<StudentProfileDto> Handle(GetCurrentStudentProfileQuery request, CancellationToken cancellationToken)
    {
        return LoadProfileAsync(currentUser.RequireStudentId(), cancellationToken);
    }

    private async System.Threading.Tasks.Task<StudentProfileDto> LoadProfileAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await dbContext.Students.AsNoTracking().Where(entity => entity.Id == studentId).Select(ToDtoExpression()).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student", studentId);
        var activeTaskCount = await dbContext.Tasks.CountAsync(task => task.AssignedStudentId == studentId && task.Status != TaskStatus.COMPLETED && task.Status != TaskStatus.CANCELLED, cancellationToken);
        var completedTaskCount = await dbContext.Tasks.CountAsync(task => task.AssignedStudentId == studentId && task.Status == TaskStatus.COMPLETED, cancellationToken);
        var skillCount = await dbContext.StudentSkills.CountAsync(skill => skill.StudentId == studentId, cancellationToken);
        var scheduleCount = await dbContext.CourseSchedules.CountAsync(schedule => schedule.StudentId == studentId, cancellationToken);
        var availabilityCount = await dbContext.Availability.CountAsync(availability => availability.StudentId == studentId, cancellationToken);
        var workload = await workloadService.GetActiveWorkloadMinutesAsync(studentId, cancellationToken: cancellationToken);
        return new StudentProfileDto(student, activeTaskCount, completedTaskCount, workload, skillCount, scheduleCount, availabilityCount);
    }

    private static System.Linq.Expressions.Expression<Func<StudentWorkforceManagement.Domain.Entities.Student, StudentDto>> ToDtoExpression()
    {
        return student => new StudentDto(student.Id, student.UserId, student.FirstName, student.LastName, student.Email, student.Department, student.IsActive, student.CreatedAt, student.UpdatedAt, student.ConcurrencyToken);
    }
}
