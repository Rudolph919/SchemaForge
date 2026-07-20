using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Contracts.V1.Components;

public sealed record AddComponentNodeRequest(
    Guid ParentNodeId, NodeAttachmentKind AttachmentKind, string? PropertyName, NodeKind? Kind);
