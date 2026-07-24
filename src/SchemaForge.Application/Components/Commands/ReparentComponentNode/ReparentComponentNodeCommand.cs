using SchemaForge.Application.Common.Messaging;
using SchemaForge.Application.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.ReparentComponentNode;

public sealed record ReparentComponentNodeCommand(
    Guid ComponentVersionId, Guid NodeId, Guid NewParentNodeId, NodeAttachmentKind? AttachmentKind, string? PropertyName)
    : ICommand<Result>;
