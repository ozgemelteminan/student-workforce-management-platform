using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Audit.DTOs;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Pagination;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Audit.Queries;

public sealed record GetAuditLogsQuery : PagedQuery, IRequest<PaginatedResult<AuditLogDto>>, IAuthorizableRequest
{
    public Guid? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}
public sealed record GetAuditLogQuery(Guid AuditLogId) : IRequest<AuditLogDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}

public sealed class AuditQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext)
    : IRequestHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>, IRequestHandler<GetAuditLogQuery, AuditLogDto>
{
    public async System.Threading.Tasks.Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogs.AsNoTracking().AsQueryable();
        if (request.UserId.HasValue) query = query.Where(log => log.UserId == request.UserId.Value);
        if (request.EntityId.HasValue) query = query.Where(log => log.EntityId == request.EntityId.Value);
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(log => log.Action == request.Action.Trim());
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(log => log.EntityType == request.EntityType.Trim());
        if (request.From.HasValue) query = query.Where(log => log.CreatedAt >= request.From.Value);
        if (request.To.HasValue) query = query.Where(log => log.CreatedAt <= request.To.Value);
        return await query.OrderByDescending(log => log.CreatedAt).Select(ToDtoExpression()).ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    public async System.Threading.Tasks.Task<AuditLogDto> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.AuditLogs.AsNoTracking().Where(log => log.Id == request.AuditLogId).Select(ToDtoExpression()).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("AuditLog", request.AuditLogId);
    }

    private static System.Linq.Expressions.Expression<Func<StudentWorkforceManagement.Domain.Entities.AuditLog, AuditLogDto>> ToDtoExpression()
    {
        return log => new AuditLogDto(log.Id, log.UserId, log.Action, log.EntityType, log.EntityId, log.OldValue, log.NewValue, log.IpAddress, log.CorrelationId, log.CreatedAt);
    }
}
