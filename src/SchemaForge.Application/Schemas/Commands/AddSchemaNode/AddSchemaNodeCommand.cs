using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.AddSchemaNode;

public sealed record AddSchemaNodeCommand(
    Guid SchemaVersionId, Guid ParentNodeId, NodeAttachmentKind AttachmentKind, string? PropertyName, NodeKind? Kind)
    : ICommand<Result<AddSchemaNodeResult>>;

public sealed record AddSchemaNodeResult(Guid NodeId);
