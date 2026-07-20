using System.Text.Json;

namespace SchemaForge.Contracts.V1.Schemas;

// Mirrors Domain's SchemaNodeContent shape exactly: the node's complete new content, not a
// partial patch. Excludes Id/PropertyName/Order (structural, not a content edit) and every
// tree-shaped field (Properties/PrefixItems/ItemsNode/CompositionBranches/IfNode/ThenNode/
// ElseNode) - those are mutated via AddSchemaNode/RemoveSchemaNode, not bulk-replaced here.
public sealed record UpdateSchemaNodeRequest(
    NodeKind? Kind,
    string? Description,
    string? Notes,
    bool IsNullable,
    bool IsRequiredByParent,
    IReadOnlyList<JsonElement> Examples,
    JsonElement? DefaultValue,
    IReadOnlyList<JsonElement>? AllowedValues,
    JsonElement? ConstValue,
    ObjectConstraintsDto? ObjectConstraints,
    ArrayConstraintsDto? ArrayConstraints,
    StringConstraintsDto? StringConstraints,
    NumericConstraintsDto? NumericConstraints,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? DependentRequired,
    CompositionKind? Composition,
    ComponentReferenceDto? ComponentReference,
    Guid? LocalDefinitionRef);
