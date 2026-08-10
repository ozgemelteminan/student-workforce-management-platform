using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Templates.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Templates.Queries;

public sealed record GetTaskTemplatesQuery : PagedQuery, IRequest<PaginatedResult<TaskTemplateDto>>, IAuthorizableRequest
{
    public Guid? CategoryId { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}
public sealed record GetTaskTemplateQuery(Guid TemplateId) : IRequest<TaskTemplateDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class TaskTemplateQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext)
    : IRequestHandler<GetTaskTemplatesQuery, PaginatedResult<TaskTemplateDto>>, IRequestHandler<GetTaskTemplateQuery, TaskTemplateDto>
{
    public async System.Threading.Tasks.Task<PaginatedResult<TaskTemplateDto>> Handle(GetTaskTemplatesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.TaskTemplates.AsNoTracking().AsQueryable();
        if (request.CategoryId.HasValue) query = query.Where(template => template.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(template => template.Title.ToLower().Contains(term));
        }
        return await query.OrderBy(template => template.Title).Select(template => new TaskTemplateDto(template.Id, template.Title, template.Description, template.CategoryId, template.DefaultPriority, template.DefaultDifficulty, template.EstimatedDurationMinutes, template.CreatedById, template.ChecklistTemplateJson, template.RequiredSkillsTemplateJson, template.CreatedAt, template.UpdatedAt)).ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async System.Threading.Tasks.Task<TaskTemplateDto> Handle(GetTaskTemplateQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TaskTemplates.AsNoTracking().Where(template => template.Id == request.TemplateId).Select(template => new TaskTemplateDto(template.Id, template.Title, template.Description, template.CategoryId, template.DefaultPriority, template.DefaultDifficulty, template.EstimatedDurationMinutes, template.CreatedById, template.ChecklistTemplateJson, template.RequiredSkillsTemplateJson, template.CreatedAt, template.UpdatedAt)).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("TaskTemplate", request.TemplateId);
    }
}
