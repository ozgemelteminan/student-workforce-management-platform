using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Settings.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Settings.Commands.UpdateSetting;

public sealed record UpdateSettingCommand(string Key, string Value, Guid ConcurrencyToken) : IRequest<SystemSettingDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Authorize.AdminOnly;
}
public sealed class UpdateSettingCommandValidator : AbstractValidator<UpdateSettingCommand>
{
    public UpdateSettingCommandValidator()
    {
        RuleFor(command => command.Key).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Value).NotNull().MaximumLength(4000);
        RuleFor(command => command.ConcurrencyToken).NotEmpty();
    }
}
public sealed class UpdateSettingCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<UpdateSettingCommand, SystemSettingDto>
{
    public async System.Threading.Tasks.Task<SystemSettingDto> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await dbContext.SystemSettings.SingleOrDefaultAsync(entity => entity.Key == request.Key, cancellationToken)
            ?? throw new NotFoundException("SystemSetting", request.Key);
        if (setting.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new ConcurrencyConflictException();
        }
        setting.Value = request.Value;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SystemSettingDto(setting.Id, setting.Key, setting.Value, setting.Description, setting.ConcurrencyToken);
    }
}
