using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Tasks.DTOs;

namespace StudentWorkforceManagement.Application.Tasks.Queries.GetTask;

public sealed record GetTaskQuery(Guid Id) : IRequest<TaskDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<StudentWorkforceManagement.Domain.Enums.UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class GetTaskQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetTaskQuery, TaskDto>
{
    public async System.Threading.Tasks.Task<TaskDto> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Task", request.Id);

        if (currentUser.IsInRole(StudentWorkforceManagement.Domain.Enums.UserRole.STUDENT) && task.AssignedStudentId != currentUser.StudentId)
        {
            throw new ForbiddenException("Students may only access their assigned tasks through this query.");
        }

        var categoryName = await dbContext.Categories.IgnoreQueryFilters()
            .Where(category => category.Id == task.CategoryId)
            .Select(category => category.Name)
            .SingleOrDefaultAsync(cancellationToken);
        var assignedStudentName = task.AssignedStudentId.HasValue
            ? await dbContext.Students.IgnoreQueryFilters()
                .Where(student => student.Id == task.AssignedStudentId.Value)
                .Select(student => (student.FirstName + " " + student.LastName).Trim())
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var createdByDisplayName = await dbContext.Users
            .Where(user => user.Id == task.CreatedById)
            .Select(user => user.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);

        return new TaskDto(task.Id, task.Title, task.Description, task.CategoryId, task.SemesterId, task.Priority, task.Difficulty, task.Status, task.CreatedById, task.AssignedStudentId, task.StartDate, task.Deadline, task.EstimatedDurationMinutes, task.CreatedAt, task.UpdatedAt, task.CompletedAt, task.ConcurrencyToken, categoryName, assignedStudentName, createdByDisplayName);
    }
}
