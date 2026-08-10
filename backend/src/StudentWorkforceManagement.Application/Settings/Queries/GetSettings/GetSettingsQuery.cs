using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Settings.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Settings.Queries.GetSettings;

public sealed record GetSettingsQuery : IRequest<IReadOnlyCollection<SystemSettingDto>>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}
public sealed class GetSettingsQueryHandler(StudentWorkforceManagement.Application.Common.Interfaces.IApplicationDbContext dbContext) : IRequestHandler<GetSettingsQuery, IReadOnlyCollection<SystemSettingDto>>
{
    public async System.Threading.Tasks.Task<IReadOnlyCollection<SystemSettingDto>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.SystemSettings.AsNoTracking().OrderBy(setting => setting.Key).Select(setting => new SystemSettingDto(setting.Id, setting.Key, setting.Value, setting.Description, setting.ConcurrencyToken)).ToListAsync(cancellationToken);
    }
}
