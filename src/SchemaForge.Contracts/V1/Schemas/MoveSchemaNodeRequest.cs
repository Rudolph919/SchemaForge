namespace SchemaForge.Contracts.V1.Schemas;

// NewParentNodeId null (the default) means a plain reorder among existing siblings - unchanged
// wire shape and behavior for every caller that only ever sent NewOrder. Set alongside
// AttachmentKind (and PropertyName, for ObjectProperty) to reparent to a different node instead;
// NewOrder is ignored in that case - a reparented node is always appended at the end of its new
// parent's collection, the same as adding a brand-new node there would be.
public sealed record MoveSchemaNodeRequest(
    int NewOrder, Guid? NewParentNodeId = null, NodeAttachmentKind? AttachmentKind = null, string? PropertyName = null);
