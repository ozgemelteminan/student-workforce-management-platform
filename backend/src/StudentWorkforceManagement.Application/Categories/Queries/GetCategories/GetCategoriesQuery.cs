using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Categories.DTOs;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery : IRequest<IReadOnlyCollection<CategoryDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}
public sealed class GetCategoriesQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext) : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Categories.AsNoTracking().OrderBy(category => category.Name).Select(category => new CategoryDto(category.Id, category.Name, category.Description)).ToListAsync(cancellationToken);
    }
}
