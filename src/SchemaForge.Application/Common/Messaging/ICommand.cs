using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Common.Messaging;

// TransactionBehavior only applies to ICommand<T> (MediatR's open-generic pipeline behavior
// registration keys off this constraint) - queries never trigger the SaveChanges wrap.
public interface ICommand<TResponse> : IRequest<TResponse>
    where TResponse : IResult;
