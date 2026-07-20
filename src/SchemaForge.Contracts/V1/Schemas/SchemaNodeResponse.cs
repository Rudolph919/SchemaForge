using System.Text.Json;

namespace SchemaForge.Contracts.V1.Schemas;

public sealed record SchemaNodeResponse(
    Guid Id,
    string? PropertyName,
    int Order,
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
    IReadOnlyList<SchemaNodeResponse> Properties,
    IReadOnlyList<SchemaNodeResponse> PrefixItems,
    SchemaNodeResponse? ItemsNode,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? DependentRequired,
    CompositionKind? Composition,
    IReadOnlyList<SchemaNodeResponse> CompositionBranches,
    SchemaNodeResponse? IfNode,
    SchemaNodeResponse? ThenNode,
    SchemaNodeResponse? ElseNode,
    ComponentReferenceDto? ComponentReference,
    Guid? LocalDefinitionRef);
