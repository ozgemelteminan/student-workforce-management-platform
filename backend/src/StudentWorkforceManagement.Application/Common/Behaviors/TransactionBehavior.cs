using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentWorkforceManagement.Application.Common.Events;
using StudentWorkforceManagement.Application.Common.Exceptions;
using StudentWorkforceManagement.Application.Common.Interfaces;

namespace StudentWorkforceManagement.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    IApplicationDbContext dbContext,
    IApplicationEventQueue eventQueue) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async System.Threading.Tasks.Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
        {
            return await next();
        }

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await eventQueue.PublishQueuedAsync(cancellationToken);
            return response;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConcurrencyConflictException(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException(exception.InnerException?.Message ?? exception.Message);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
