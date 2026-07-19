using FluentValidation;
using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var error = Error.Validation(
            "Validation.Failed", string.Join(" ", failures.Select(f => f.ErrorMessage)));

        return CreateFailure(error);
    }

    // TResponse is always Result or Result<T> by convention (Step 1 §6). They can't share a
    // common base (structs, no inheritance - see the SharedKernel Result.cs comment), so the
    // failure has to be constructed via reflection rather than a direct cast or factory call.
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
            $"{responseType.Name} must be Result or Result<T> to use ValidationBehavior.");
    }
}
