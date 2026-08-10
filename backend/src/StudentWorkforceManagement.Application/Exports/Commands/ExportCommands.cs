using FluentValidation;
using MediatR;
using StudentWorkforceManagement.Application.Common.Security;
using StudentWorkforceManagement.Application.Exports.DTOs;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Application.Exports.Commands;

public sealed record RequestExportCommand(ExportType Type, ExportFormat Format, Guid? ScopeId = null) : IRequest<ExportRequestContractDto>, IAuthorizableRequest
{
    public IReadOnlyCollection<UserRole> RequiredRoles => Type == ExportType.PersonalData ? Authorize.AnyRole : Authorize.AdminOnly;
}

public sealed class RequestExportCommandValidator : AbstractValidator<RequestExportCommand>
{
    public RequestExportCommandValidator()
    {
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Format).IsInEnum();
    }
}

public sealed class ExportCommandHandler(ICurrentUserService currentUser) : IRequestHandler<RequestExportCommand, ExportRequestContractDto>
{
    public System.Threading.Tasks.Task<ExportRequestContractDto> Handle(RequestExportCommand request, CancellationToken cancellationToken)
    {
        var requestedBy = currentUser.RequireUserId();
        var boundary = request.Type == ExportType.PersonalData ? "Own personal data only unless ADMIN explicitly scopes an authorized user." : "Administrative aggregate/report export.";
        return System.Threading.Tasks.Task.FromResult(new ExportRequestContractDto(request.Type, request.Format, requestedBy, request.ScopeId, boundary, "No ExportRequest/ExportJob persistence entity exists yet for asynchronous status tracking."));
    }
}
