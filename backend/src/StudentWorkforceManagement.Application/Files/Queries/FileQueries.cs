using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Application.Files.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Files.Queries;

public sealed record GetDepartmentFilesQuery : PagedQuery, IRequest<PaginatedResult<DepartmentFileDto>>, IAuthorizableRequest
{
    public Guid? FolderId { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetFileFoldersQuery(Guid? ParentFolderId = null) : IRequest<IReadOnlyCollection<FileFolderDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetDepartmentFileDownloadQuery(Guid FileId) : IRequest<AuthorizedDownloadDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class FileQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext, IFileStorage storage)
    : IRequestHandler<GetDepartmentFilesQuery, PaginatedResult<DepartmentFileDto>>, IRequestHandler<GetFileFoldersQuery, IReadOnlyCollection<FileFolderDto>>, IRequestHandler<GetDepartmentFileDownloadQuery, AuthorizedDownloadDto>
{
    public async System.Threading.Tasks.Task<PaginatedResult<DepartmentFileDto>> Handle(GetDepartmentFilesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DepartmentFiles.AsNoTracking().Where(file => file.FileStatus != FileStatus.DELETED);
        if (request.FolderId.HasValue)
        {
            query = query.Where(file => file.FolderId == request.FolderId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(file => file.File.FileName.ToLower().Contains(term));
        }
        query = (request.SortBy?.ToLowerInvariant(), request.SortDirection?.ToLowerInvariant()) switch
        {
            ("name", "desc") => query.OrderByDescending(file => file.File.FileName),
            ("size", "desc") => query.OrderByDescending(file => file.File.FileSize),
            ("created", _) => query.OrderBy(file => file.CreatedAt),
            ("name", _) => query.OrderBy(file => file.File.FileName),
            ("size", _) => query.OrderBy(file => file.File.FileSize),
            _ => query.OrderByDescending(file => file.CreatedAt)
        };
        return await query.Select(file => new DepartmentFileDto(file.Id, file.FolderId, file.UploadedById, file.File.FileName, file.File.StorageKey, file.File.FileSize, file.File.MimeType, file.File.FileExtension, file.File.ContentHash, file.FileStatus, file.ConfirmedAt, file.CreatedAt))
            .ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyCollection<FileFolderDto>> Handle(GetFileFoldersQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.FileFolders.AsNoTracking()
            .Where(folder => folder.ParentFolderId == request.ParentFolderId)
            .OrderBy(folder => folder.Name)
            .Select(folder => new FileFolderDto(folder.Id, folder.ParentFolderId, folder.Name, folder.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<AuthorizedDownloadDto> Handle(GetDepartmentFileDownloadQuery request, CancellationToken cancellationToken)
    {
        var file = await dbContext.DepartmentFiles.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == request.FileId, cancellationToken)
            ?? throw new NotFoundException("DepartmentFile", request.FileId);
        if (file.FileStatus != FileStatus.CONFIRMED)
        {
            throw new ConflictException("Only confirmed files can be downloaded.");
        }
        var target = await storage.CreateDownloadTargetAsync(file.File.StorageKey, cancellationToken);
        return new AuthorizedDownloadDto(file.Id, file.File.StorageKey, target.DownloadUrl, target.ExpiresAt);
    }
}
