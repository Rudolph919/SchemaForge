using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.RemoveSchemaNode;

public sealed record RemoveSchemaNodeCommand(Guid SchemaVersionId, Guid NodeId, uint ExpectedVersion) : ICommand<Result>;
