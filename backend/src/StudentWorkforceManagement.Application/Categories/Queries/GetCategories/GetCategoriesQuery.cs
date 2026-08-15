using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Categories.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyCollection<CategoryDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed record GetCategoryQuery(Guid CategoryId) : IRequest<CategoryDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class GetCategoriesQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
    , IRequestHandler<GetCategoryQuery, CategoryDto>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        if (request.IncludeInactive && !currentUser.IsInRole(UserRole.ADMIN))
        {
            throw new ForbiddenException();
        }

        var query = dbContext.Categories.AsNoTracking();
        if (!request.IncludeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        return await query.OrderBy(category => category.Name).Select(category => new CategoryDto(category.Id, category.Name, category.Description, category.IsActive)).ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<CategoryDto> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Categories.AsNoTracking()
            .Where(category => category.Id == request.CategoryId)
            .Select(category => new CategoryDto(category.Id, category.Name, category.Description, category.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Category", request.CategoryId);
    }
}
