using MediatR;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Security;

namespace StudentWorkforceManagement.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async System.Threading.Tasks.Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IAuthorizableRequest authorizableRequest)
        {
            return await next();
        }

        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication is required for this request.");
        }

        if (authorizableRequest.RequiredRoles.Count > 0 && !authorizableRequest.RequiredRoles.Any(currentUser.IsInRole))
        {
            throw new ForbiddenException();
        }

        return await next();
    }
}
