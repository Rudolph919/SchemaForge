using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Common.Exceptions;
using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Common.Behaviors;

// Handlers stage changes via repository Add calls (no SaveChanges of their own) - this behavior
// is the single place SaveChanges is actually called, and only when the handler succeeded. If
// the handler failed, nothing was ever sent to the database (Add only touches the in-memory
// change tracker), so there's nothing to roll back - "commit or don't call SaveChanges" is
// sufficient here without needing an explicit transaction object at this layer.
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (!response.IsSuccess)
        {
            return response;
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Step 6 §1.5: the If-Match value a handler applied to a tracked entity's
            // OriginalValue (via the matching repository's ApplyExpectedVersion) didn't match
            // what's actually in the database - someone else updated the resource in between.
            return CreateFailure(Error.Conflict(
                "Concurrency.Conflict", "This resource was modified by someone else. Reload and try again."));
        }

        return response;
    }

    // TResponse is always Result or Result<T> by convention (Step 1 §6) - same reflection-based
    // construction ValidationBehavior already uses, for the same reason (structs can't share a
    // common base to construct a failure through directly).
    private static TResponse CreateFailure(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(nameof(Result<object>.Failure), [typeof(Error)])!;
            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"{responseType.Name} must be Result or Result<T> to use TransactionBehavior.");
    }
}
