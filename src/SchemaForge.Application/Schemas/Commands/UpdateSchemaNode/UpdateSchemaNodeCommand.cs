using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.UpdateSchemaNode;

public sealed record UpdateSchemaNodeCommand(
    Guid SchemaVersionId, Guid NodeId, SchemaNodeContent Content, uint ExpectedVersion) : ICommand<Result>;
