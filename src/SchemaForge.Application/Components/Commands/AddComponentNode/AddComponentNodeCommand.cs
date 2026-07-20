using SchemaForge.Application.Common.Messaging;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.AddComponentNode;

public sealed record AddComponentNodeCommand(
    Guid ComponentVersionId, Guid ParentNodeId, NodeAttachmentKind AttachmentKind, string? PropertyName, NodeKind? Kind)
    : ICommand<Result<AddComponentNodeResult>>;

public sealed record AddComponentNodeResult(Guid NodeId);
