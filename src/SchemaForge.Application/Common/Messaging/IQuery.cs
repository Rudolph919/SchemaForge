using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<TResponse>
    where TResponse : IResult;
