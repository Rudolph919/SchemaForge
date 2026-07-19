using MediatR;
using SchemaForge.Application.Common.Abstractions;
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

        if (response.IsSuccess)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
