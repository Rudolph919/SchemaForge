using System.Text.Json;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Infrastructure.Persistence.Serialization;

// SchemaVersion.RootNode/LocalDefinitions are persisted as opaque jsonb columns via a value
// converter (EF Core's own native JSON owned-type mapping - OwnsOne/OwnsMany(...).ToJson() -
// cannot represent a genuinely self-referential recursive structure: configuring it produces
// infinite recursion at model-building time, confirmed with a throwaway script against real
// Postgres before writing this). Rebuilds the domain tree through SchemaNode/LocalDefinition's
// internal Rehydrate/ApplyContent/Add*/Set* API (Infrastructure has access via
// InternalsVisibleTo) rather than any reflection trick.
public static class SchemaNodeJsonConverter
{
    private static readonly JsonSerializerOptions Options = new();

    public static string SerializeNode(SchemaNode node) => JsonSerializer.Serialize(ToDto(node), Options);

    public static SchemaNode DeserializeNode(string json) =>
        FromDto(JsonSerializer.Deserialize<NodeDto>(json, Options)!);

    public static string SerializeLocalDefinitions(IReadOnlyList<LocalDefinition> definitions) =>
        JsonSerializer.Serialize(definitions.Select(ToDto).ToList(), Options);

    public static List<LocalDefinition> DeserializeLocalDefinitions(string json) =>
        JsonSerializer.Deserialize<List<LocalDefinitionDto>>(json, Options)!.Select(FromDto).ToList();

    private static NodeDto ToDto(SchemaNode node) => new(
        node.Id,
        node.PropertyName,
        node.Order,
        node.Kind,
        node.Description,
        node.Notes,
        node.IsNullable,
        node.IsRequiredByParent,
        [.. node.Examples.Select(e => e.RawJson)],
        node.DefaultValue?.RawJson,
        node.AllowedValues?.Select(e => e.RawJson).ToList(),
        node.ConstValue?.RawJson,
        node.ObjectConstraints,
        node.ArrayConstraints,
        node.StringConstraints,
        node.NumericConstraints,
        [.. node.Properties.Select(ToDto)],
        [.. node.PrefixItems.Select(ToDto)],
        node.ItemsNode is null ? null : ToDto(node.ItemsNode),
        node.DependentRequired?.ToDictionary(kv => kv.Key, kv => kv.Value.ToList()),
        node.Composition,
        [.. node.CompositionBranches.Select(ToDto)],
        node.IfNode is null ? null : ToDto(node.IfNode),
        node.ThenNode is null ? null : ToDto(node.ThenNode),
        node.ElseNode is null ? null : ToDto(node.ElseNode),
        node.ComponentReference is null ? null : ToDto(node.ComponentReference),
        node.LocalDefinitionRef);

    private static SchemaNode FromDto(NodeDto dto)
    {
        var node = SchemaNode.Rehydrate(dto.Id, dto.Kind, dto.PropertyName, dto.Order);

        var content = new SchemaNodeContent(
            dto.Kind,
            dto.Description,
            dto.Notes,
            dto.IsNullable,
            dto.IsRequiredByParent,
            [.. dto.Examples.Select(JsonLiteral.FromRawJson)],
            dto.DefaultValue is null ? null : JsonLiteral.FromRawJson(dto.DefaultValue),
            dto.AllowedValues?.Select(JsonLiteral.FromRawJson).ToList(),
            dto.ConstValue is null ? null : JsonLiteral.FromRawJson(dto.ConstValue),
            dto.ObjectConstraints,
            dto.ArrayConstraints,
            dto.StringConstraints,
            dto.NumericConstraints,
            dto.DependentRequired?.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value),
            dto.Composition,
            dto.ComponentReference is null ? null : FromDto(dto.ComponentReference),
            dto.LocalDefinitionRef);

        node.ApplyContent(content);

        foreach (var propertyDto in dto.Properties)
        {
            node.AddProperty(FromDto(propertyDto));
        }

        foreach (var prefixItemDto in dto.PrefixItems)
        {
            node.AddPrefixItem(FromDto(prefixItemDto));
        }

        if (dto.ItemsNode is not null)
        {
            node.SetItemsNode(FromDto(dto.ItemsNode));
        }

        foreach (var branchDto in dto.CompositionBranches)
        {
            node.AddCompositionBranch(FromDto(branchDto));
        }

        if (dto.IfNode is not null) node.SetIfNode(FromDto(dto.IfNode));
        if (dto.ThenNode is not null) node.SetThenNode(FromDto(dto.ThenNode));
        if (dto.ElseNode is not null) node.SetElseNode(FromDto(dto.ElseNode));

        return node;
    }

    private static LocalDefinitionDto ToDto(LocalDefinition definition) =>
        new(definition.Id, definition.Name, ToDto(definition.RootNode));

    private static LocalDefinition FromDto(LocalDefinitionDto dto) =>
        LocalDefinition.Rehydrate(dto.Id, dto.Name, FromDto(dto.RootNode));

    private static ComponentReferenceDto ToDto(ComponentReference reference) => new(
        reference.ComponentVersionId,
        reference.Constraint.Kind,
        reference.Constraint.Version is null ? null : ToDto(reference.Constraint.Version));

    private static ComponentReference FromDto(ComponentReferenceDto dto)
    {
        var constraint = dto.ConstraintKind switch
        {
            VersionConstraintKind.ExactVersion => VersionConstraint.ExactVersion(FromDto(dto.Version!)),
            VersionConstraintKind.MinimumVersion => VersionConstraint.MinimumVersion(FromDto(dto.Version!)),
            VersionConstraintKind.Latest => VersionConstraint.Latest,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto.ConstraintKind, "Unknown version constraint kind."),
        };

        return new ComponentReference(dto.ComponentVersionId, constraint);
    }

    private static SemVerDto ToDto(SemVer semVer) => new(semVer.Major, semVer.Minor, semVer.Patch);

    private static SemVer FromDto(SemVerDto dto) => SemVer.Create(dto.Major, dto.Minor, dto.Patch);

    private sealed record NodeDto(
        Guid Id,
        string? PropertyName,
        int Order,
        NodeKind? Kind,
        string? Description,
        string? Notes,
        bool IsNullable,
        bool IsRequiredByParent,
        List<string> Examples,
        string? DefaultValue,
        List<string>? AllowedValues,
        string? ConstValue,
        ObjectConstraints? ObjectConstraints,
        ArrayConstraints? ArrayConstraints,
        StringConstraints? StringConstraints,
        NumericConstraints? NumericConstraints,
        List<NodeDto> Properties,
        List<NodeDto> PrefixItems,
        NodeDto? ItemsNode,
        Dictionary<string, List<string>>? DependentRequired,
        CompositionKind? Composition,
        List<NodeDto> CompositionBranches,
        NodeDto? IfNode,
        NodeDto? ThenNode,
        NodeDto? ElseNode,
        ComponentReferenceDto? ComponentReference,
        Guid? LocalDefinitionRef);

    private sealed record ComponentReferenceDto(Guid ComponentVersionId, VersionConstraintKind ConstraintKind, SemVerDto? Version);

    private sealed record SemVerDto(int Major, int Minor, int Patch);

    private sealed record LocalDefinitionDto(Guid Id, string Name, NodeDto RootNode);
}
