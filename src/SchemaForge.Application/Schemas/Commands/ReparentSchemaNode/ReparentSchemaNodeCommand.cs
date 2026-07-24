using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.ReparentSchemaNode;

public sealed record ReparentSchemaNodeCommand(
    Guid SchemaVersionId, Guid NodeId, Guid NewParentNodeId, NodeAttachmentKind? AttachmentKind, string? PropertyName)
    : ICommand<Result>;
