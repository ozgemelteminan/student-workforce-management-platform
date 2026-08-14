using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Files.DTOs;
using StudentWorkforceManagement.Application.Files.Services;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Application.Files.Commands;

public sealed record InitiateDepartmentFileUploadCommand(Guid? FolderId, string FileName, long FileSize, string MimeType, string FileExtension, string? ContentHash) : IRequest<FileUploadIntentDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record CompleteDepartmentFileUploadCommand(Guid FileId) : IRequest<DepartmentFileDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record DeleteDepartmentFileCommand(Guid FileId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record CreateFileFolderCommand(Guid? ParentFolderId, string Name) : IRequest<FileFolderDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record RenameFileFolderCommand(Guid FolderId, string Name) : IRequest<FileFolderDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed record DeleteFileFolderCommand(Guid FolderId) : IRequest<Unit>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.StaffTaskManagement;
}

public sealed class InitiateDepartmentFileUploadCommandValidator : AbstractValidator<InitiateDepartmentFileUploadCommand>
{
    public InitiateDepartmentFileUploadCommandValidator()
    {
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(255);
        RuleFor(command => command.FileSize).InclusiveBetween(1, UploadFilePolicyOptions.OneGigabyteInBytes);
        RuleFor(command => command.MimeType).NotEmpty().MaximumLength(150);
        RuleFor(command => command.FileExtension).NotEmpty().MaximumLength(20);
        RuleFor(command => command.ContentHash).MaximumLength(128);
    }
}

public sealed class FileFolderCommandValidator : AbstractValidator<CreateFileFolderCommand>
{
    public FileFolderCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class FileCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IFileStorage storage, IUploadFilePolicy uploadFilePolicy, IUtcClock clock)
    : IRequestHandler<InitiateDepartmentFileUploadCommand, FileUploadIntentDto>,
      IRequestHandler<CompleteDepartmentFileUploadCommand, DepartmentFileDto>,
      IRequestHandler<DeleteDepartmentFileCommand, Unit>,
      IRequestHandler<CreateFileFolderCommand, FileFolderDto>,
      IRequestHandler<RenameFileFolderCommand, FileFolderDto>,
      IRequestHandler<DeleteFileFolderCommand, Unit>
{
    public async System.Threading.Tasks.Task<FileUploadIntentDto> Handle(InitiateDepartmentFileUploadCommand request, CancellationToken cancellationToken)
    {
        var upload = uploadFilePolicy.ValidatePendingUpload(request.FileName, request.FileSize, request.MimeType, request.FileExtension);
        if (request.FolderId.HasValue && !await dbContext.FileFolders.AnyAsync(folder => folder.Id == request.FolderId.Value, cancellationToken))
        {
            throw new NotFoundException("FileFolder", request.FolderId.Value);
        }
        var target = await storage.CreateUploadTargetAsync(new UploadTargetRequest(upload.FileName, upload.FileSizeBytes, upload.MimeType, upload.FileExtension, "department-files", false), cancellationToken);
        var file = new DepartmentFile
        {
            Id = Guid.NewGuid(),
            FolderId = request.FolderId,
            UploadedById = currentUser.RequireUserId(),
            FileStatus = FileStatus.UPLOAD_PENDING,
            File = new FileMetadata
            {
                FileName = upload.FileName,
                StorageKey = target.StorageKey,
                FileSize = upload.FileSizeBytes,
                MimeType = upload.MimeType,
                FileExtension = upload.FileExtension,
                ContentHash = request.ContentHash
            }
        };
        dbContext.DepartmentFiles.Add(file);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new FileUploadIntentDto(file.Id, target.StorageKey, file.File.FileName, file.File.FileSize, file.File.MimeType, file.File.FileExtension, file.FileStatus, target.UploadUrl, target.UploadMethod, target.RequiredHeaders ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), target.ExpiresAt);
    }

    public async System.Threading.Tasks.Task<DepartmentFileDto> Handle(CompleteDepartmentFileUploadCommand request, CancellationToken cancellationToken)
    {
        var file = await dbContext.DepartmentFiles.SingleOrDefaultAsync(entity => entity.Id == request.FileId, cancellationToken)
            ?? throw new NotFoundException("DepartmentFile", request.FileId);
        if (file.FileStatus == FileStatus.CONFIRMED)
        {
            return ToDto(file);
        }
        if (file.FileStatus != FileStatus.UPLOAD_PENDING && file.FileStatus != FileStatus.UPLOADED)
        {
            throw new ConflictException("File is not in a completable state.");
        }
        var metadata = await storage.GetMetadataAsync(file.File.StorageKey, cancellationToken)
            ?? throw new ConflictException("Uploaded object metadata could not be verified.");
        uploadFilePolicy.ValidateStoredObject(file.File, metadata);
        if (!string.IsNullOrWhiteSpace(file.File.ContentHash) && !string.Equals(file.File.ContentHash, metadata.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Uploaded object content hash does not match the pending file metadata.");
        }
        file.FileStatus = FileStatus.CONFIRMED;
        file.ConfirmedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(file);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteDepartmentFileCommand request, CancellationToken cancellationToken)
    {
        var file = await dbContext.DepartmentFiles.SingleOrDefaultAsync(entity => entity.Id == request.FileId, cancellationToken)
            ?? throw new NotFoundException("DepartmentFile", request.FileId);
        await storage.DeleteAsync(file.File.StorageKey, cancellationToken);
        file.FileStatus = FileStatus.DELETED;
        file.DeletedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async System.Threading.Tasks.Task<FileFolderDto> Handle(CreateFileFolderCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentFolderId.HasValue && !await dbContext.FileFolders.AnyAsync(folder => folder.Id == request.ParentFolderId.Value, cancellationToken))
        {
            throw new NotFoundException("FileFolder", request.ParentFolderId.Value);
        }
        var folder = new FileFolder { Id = Guid.NewGuid(), ParentFolderId = request.ParentFolderId, Name = request.Name.Trim() };
        dbContext.FileFolders.Add(folder);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(folder);
    }

    public async System.Threading.Tasks.Task<FileFolderDto> Handle(RenameFileFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await dbContext.FileFolders.SingleOrDefaultAsync(entity => entity.Id == request.FolderId, cancellationToken)
            ?? throw new NotFoundException("FileFolder", request.FolderId);
        folder.Name = request.Name.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(folder);
    }

    public async System.Threading.Tasks.Task<Unit> Handle(DeleteFileFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await dbContext.FileFolders.SingleOrDefaultAsync(entity => entity.Id == request.FolderId, cancellationToken)
            ?? throw new NotFoundException("FileFolder", request.FolderId);
        var hasChildren = await dbContext.FileFolders.AnyAsync(child => child.ParentFolderId == request.FolderId, cancellationToken);
        var hasFiles = await dbContext.DepartmentFiles.AnyAsync(file => file.FolderId == request.FolderId, cancellationToken);
        if (hasChildren || hasFiles)
        {
            throw new ConflictException("Only empty folders can be deleted.");
        }
        folder.DeletedAt = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private static DepartmentFileDto ToDto(DepartmentFile file) => new(file.Id, file.FolderId, file.UploadedById, file.File.FileName, file.File.StorageKey, file.File.FileSize, file.File.MimeType, file.File.FileExtension, file.File.ContentHash, file.FileStatus, file.ConfirmedAt, file.CreatedAt);
    private static FileFolderDto ToDto(FileFolder folder) => new(folder.Id, folder.ParentFolderId, folder.Name, folder.CreatedAt);
}
