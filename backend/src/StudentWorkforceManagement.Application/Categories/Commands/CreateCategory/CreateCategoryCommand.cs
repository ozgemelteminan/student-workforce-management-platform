using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Categories.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;
using Category = StudentWorkforceManagement.Domain.Entities.Category;

namespace StudentWorkforceManagement.Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string? Description) : IRequest<CategoryDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record DeactivateCategoryCommand(Guid CategoryId) : IRequest<CategoryDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed record ReactivateCategoryCommand(Guid CategoryId) : IRequest<CategoryDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name).Must(value => string.IsNullOrWhiteSpace(value) == false).WithMessage("Category name is required.").MaximumLength(120);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).Must(value => string.IsNullOrWhiteSpace(value) == false).WithMessage("Category name is required.").MaximumLength(120);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

public sealed class CategoryCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>,
      IRequestHandler<UpdateCategoryCommand, CategoryDto>,
      IRequestHandler<DeactivateCategoryCommand, CategoryDto>,
      IRequestHandler<ReactivateCategoryCommand, CategoryDto>
{
    public async System.Threading.Tasks.Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        await EnsureNameAvailableAsync(name, null, cancellationToken);
        var category = new Category { Id = Guid.NewGuid(), Name = name, Description = CleanDescription(request.Description), IsActive = true };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(category);
    }

    public async System.Threading.Tasks.Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category", request.CategoryId);
        var name = NormalizeName(request.Name);
        await EnsureNameAvailableAsync(name, request.CategoryId, cancellationToken);
        category.Name = name;
        category.Description = CleanDescription(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(category);
    }

    public async System.Threading.Tasks.Task<CategoryDto> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category", request.CategoryId);
        category.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(category);
    }

    public async System.Threading.Tasks.Task<CategoryDto> Handle(ReactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category", request.CategoryId);
        await EnsureNameAvailableAsync(category.Name, category.Id, cancellationToken);
        category.IsActive = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(category);
    }

    private async System.Threading.Tasks.Task EnsureNameAvailableAsync(string name, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalized = name.ToUpperInvariant();
        var exists = await dbContext.Categories.AnyAsync(category =>
            category.Name.ToUpper() == normalized && (!currentId.HasValue || category.Id != currentId.Value), cancellationToken);
        if (exists)
        {
            throw new ConflictException("Category name already exists.");
        }
    }

    private static string NormalizeName(string name) => name.Trim();

    private static string? CleanDescription(string? description) => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static CategoryDto ToDto(Category category) => new(category.Id, category.Name, category.Description, category.IsActive);
}
