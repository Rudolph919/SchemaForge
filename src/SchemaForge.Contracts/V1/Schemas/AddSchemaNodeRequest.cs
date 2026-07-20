namespace SchemaForge.Contracts.V1.Schemas;

public sealed record AddSchemaNodeRequest(
    Guid ParentNodeId, NodeAttachmentKind AttachmentKind, string? PropertyName, NodeKind? Kind);
