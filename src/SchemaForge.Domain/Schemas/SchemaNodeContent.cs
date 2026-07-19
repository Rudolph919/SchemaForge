using SchemaForge.Domain.Schemas.ValueObjects;

namespace SchemaForge.Domain.Schemas;

// The full set of a SchemaNode's directly-settable scalar/leaf fields, bundled as one parameter
// object for SchemaVersion.UpdateNode. Deliberately whole-state, not a delta/patch (matching
// UpdateProjectDetailsCommand/UpdateTeamDetailsCommand's pattern elsewhere in this codebase) -
// the caller always supplies the complete intended state for these fields, sidestepping the
// "does null mean unset or clear" ambiguity a partial-update shape would introduce here, where
// most of these fields are already legitimately nullable on their own terms.
//
// Deliberately excludes: Id (identity), PropertyName/Order (structural position, managed by the
// attach/move operations, not a content edit), and every tree-shaped field (Properties,
// PrefixItems, ItemsNode, CompositionBranches, IfNode/ThenNode/ElseNode) - those are mutated via
// their own dedicated SchemaVersion methods, not bulk-replaced here.
public sealed record SchemaNodeContent(
    NodeKind? Kind,
    string? Description,
    string? Notes,
    bool IsNullable,
    bool IsRequiredByParent,
    IReadOnlyList<JsonLiteral> Examples,
    JsonLiteral? DefaultValue,
    IReadOnlyList<JsonLiteral>? AllowedValues,
    JsonLiteral? ConstValue,
    ObjectConstraints? ObjectConstraints,
    ArrayConstraints? ArrayConstraints,
    StringConstraints? StringConstraints,
    NumericConstraints? NumericConstraints,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? DependentRequired,
    CompositionKind? Composition,
    ComponentReference? ComponentReference,
    Guid? LocalDefinitionRef)
{
    public static SchemaNodeContent Empty(NodeKind? kind) => new(
        kind, null, null, false, false, [], null, null, null, null, null, null, null, null, null, null, null);
}
