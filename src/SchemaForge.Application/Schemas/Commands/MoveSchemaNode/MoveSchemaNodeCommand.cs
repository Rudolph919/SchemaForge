using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.MoveSchemaNode;

public sealed record MoveSchemaNodeCommand(Guid SchemaVersionId, Guid NodeId, int NewOrder) : ICommand<Result>;
