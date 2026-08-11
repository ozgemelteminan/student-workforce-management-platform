using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Common.Storage;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Application.Exports.DTOs;
using StudentWorkforceManagement.Domain.Entities;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Exports.Commands;

public sealed record RequestExportCommand(ExportType Type, ExportFormat Format, Guid? ScopeId = null) : IRequest<ExportAcceptedDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Type == ExportType.PersonalData ? Authorize.AnyRole : Authorize.AdminOnly;
}

public sealed record GetExportsQuery : PagedQuery, IRequest<PaginatedResult<ExportRequestDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetExportQuery(Guid ExportRequestId) : IRequest<ExportRequestDto>, IAuthorizableRequest, IExportRequestId
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed record GetExportDownloadQuery(Guid ExportRequestId) : IRequest<ExportDownloadDto>, IAuthorizableRequest, IExportRequestId
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AnyRole;
}

public sealed class RequestExportCommandValidator : AbstractValidator<RequestExportCommand>
{
    public RequestExportCommandValidator()
    {
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Format).IsInEnum();
        RuleFor(command => command.ScopeId).Empty().When(command => command.Type == ExportType.PersonalData).WithMessage("Personal data exports are scoped to the current authenticated user.");
    }
}

public sealed class ExportRequestIdValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : IExportRequestId
{
    public ExportRequestIdValidator()
    {
        RuleFor(request => request.ExportRequestId).NotEmpty();
    }
}

public interface IExportRequestId
{
    Guid ExportRequestId { get; }
}

public sealed class ExportCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    IExportJobScheduler scheduler,
    IFileStorage storage,
    IUtcClock clock)
    : IRequestHandler<RequestExportCommand, ExportAcceptedDto>,
      IRequestHandler<GetExportsQuery, PaginatedResult<ExportRequestDto>>,
      IRequestHandler<GetExportQuery, ExportRequestDto>,
      IRequestHandler<GetExportDownloadQuery, ExportDownloadDto>
{
    public async System.Threading.Tasks.Task<ExportAcceptedDto> Handle(RequestExportCommand request, CancellationToken cancellationToken)
    {
        var requestingUserId = currentUser.RequireUserId();
        var entity = new ExportRequest
        {
            Id = Guid.NewGuid(),
            RequestingUserId = requestingUserId,
            AuthorizedUserId = request.Type == ExportType.PersonalData ? requestingUserId : null,
            ExportType = request.Type,
            Format = request.Format,
            ScopeId = request.ScopeId,
            Status = ExportStatus.QUEUED,
            RequestedAt = clock.UtcNow
        };

        dbContext.ExportRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await scheduler.EnqueueAsync(entity.Id, cancellationToken);

        return new ExportAcceptedDto(entity.Id, entity.Status, new Uri($"/api/v1/exports/{entity.Id:D}", UriKind.Relative));
    }

    public async System.Threading.Tasks.Task<PaginatedResult<ExportRequestDto>> Handle(GetExportsQuery request, CancellationToken cancellationToken)
    {
        var query = ApplyVisibility(dbContext.ExportRequests.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(entity => entity.ExportType.ToString().ToLower().Contains(term) || entity.Status.ToString().ToLower().Contains(term));
        }

        return await query.OrderByDescending(entity => entity.RequestedAt)
            .Select(entity => ToDto(entity))
            .ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async System.Threading.Tasks.Task<ExportRequestDto> Handle(GetExportQuery request, CancellationToken cancellationToken)
    {
        var entity = await LoadVisibleExportAsync(request.ExportRequestId, track: true, cancellationToken);
        await MarkExpiredIfNeededAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    public async System.Threading.Tasks.Task<ExportDownloadDto> Handle(GetExportDownloadQuery request, CancellationToken cancellationToken)
    {
        var entity = await LoadVisibleExportAsync(request.ExportRequestId, track: true, cancellationToken);
        await MarkExpiredIfNeededAsync(entity, cancellationToken);

        if (entity.Status != ExportStatus.COMPLETED)
        {
            throw new ConflictException($"Export is {entity.Status} and is not available for download.");
        }

        if (string.IsNullOrWhiteSpace(entity.ArtifactStorageKey) ||
            string.IsNullOrWhiteSpace(entity.ArtifactFileName) ||
            !entity.ArtifactFileSize.HasValue ||
            string.IsNullOrWhiteSpace(entity.ArtifactMimeType))
        {
            throw new ConflictException("Completed export is missing artifact metadata.");
        }

        var download = await storage.CreateDownloadTargetAsync(entity.ArtifactStorageKey, cancellationToken);
        return new ExportDownloadDto(entity.Id, entity.ArtifactStorageKey, entity.ArtifactFileName, entity.ArtifactFileSize.Value, entity.ArtifactMimeType, download.DownloadUrl, download.ExpiresAt);
    }

    private IQueryable<ExportRequest> ApplyVisibility(IQueryable<ExportRequest> query)
    {
        var userId = currentUser.RequireUserId();
        if (currentUser.IsInRole(UserRole.ADMIN))
        {
            return query;
        }

        return query.Where(entity => entity.RequestingUserId == userId || entity.AuthorizedUserId == userId);
    }

    private async System.Threading.Tasks.Task<ExportRequest> LoadVisibleExportAsync(Guid id, bool track, CancellationToken cancellationToken)
    {
        var query = track ? dbContext.ExportRequests.AsQueryable() : dbContext.ExportRequests.AsNoTracking();
        var entity = await ApplyVisibility(query).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity ?? throw new NotFoundException("ExportRequest", id);
    }

    private async System.Threading.Tasks.Task MarkExpiredIfNeededAsync(ExportRequest entity, CancellationToken cancellationToken)
    {
        if (entity.Status == ExportStatus.COMPLETED && entity.ExpiresAt.HasValue && entity.ExpiresAt <= clock.UtcNow)
        {
            entity.Status = ExportStatus.EXPIRED;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static ExportRequestDto ToDto(ExportRequest entity)
    {
        return new ExportRequestDto(
            entity.Id,
            entity.RequestingUserId,
            entity.ExportType,
            entity.Format,
            entity.Status,
            entity.ScopeId,
            entity.RequestedAt,
            entity.ProcessingStartedAt,
            entity.CompletedAt,
            entity.FailedAt,
            entity.ExpiresAt,
            entity.FailureReason,
            entity.ArtifactFileName,
            entity.ArtifactFileSize,
            entity.ArtifactMimeType,
            entity.ConcurrencyToken);
    }
}
