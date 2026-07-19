namespace SchemaForge.Application.Schemas;

// SchemaVersion exposes a separate, purpose-specific method per attachment point (AddObjectProperty,
// AddArrayPrefixItem, SetArrayItemsNode, AddCompositionBranch, SetConditionalNode) rather than one
// generic "AddNode" - this enum is what lets a single AddSchemaNodeCommand/endpoint dispatch to
// whichever one the caller actually means, without collapsing them into a vaguer domain method.
public enum NodeAttachmentKind
{
    ObjectProperty,
    ArrayPrefixItem,
    ArrayItems,
    CompositionBranch,
    ConditionalIf,
    ConditionalThen,
    ConditionalElse
}
