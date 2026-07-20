using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

internal static class DocumentationModelBuilder
{
    public static IReadOnlyList<DocumentationField> Build(SchemaVersion version)
    {
        var fields = new List<DocumentationField>();
        Walk(version.RootNode, "$", 0, isRequired: false, fields);

        foreach (var definition in version.LocalDefinitions)
        {
            Walk(definition.RootNode, $"$defs.{definition.Name}", 0, isRequired: false, fields);
        }

        return fields;
    }

    private static void Walk(SchemaNode node, string path, int depth, bool isRequired, List<DocumentationField> fields)
    {
        fields.Add(new DocumentationField(
            path, node.PropertyName, depth, DescribeKind(node), isRequired, node.IsNullable, node.Description, DescribeConstraints(node)));

        foreach (var child in node.Properties.OrderBy(p => p.Order))
        {
            Walk(child, $"{path}.{child.PropertyName}", depth + 1, child.IsRequiredByParent, fields);
        }

        for (var i = 0; i < node.PrefixItems.Count; i++)
        {
            Walk(node.PrefixItems[i], $"{path}[{i}]", depth + 1, isRequired: false, fields);
        }

        if (node.ItemsNode is not null)
        {
            Walk(node.ItemsNode, $"{path}[]", depth + 1, isRequired: false, fields);
        }

        for (var i = 0; i < node.CompositionBranches.Count; i++)
        {
            Walk(node.CompositionBranches[i], $"{path}.{DescribeComposition(node.Composition)}[{i}]", depth + 1, isRequired: false, fields);
        }

        if (node.IfNode is not null) Walk(node.IfNode, $"{path}.if", depth + 1, isRequired: false, fields);
        if (node.ThenNode is not null) Walk(node.ThenNode, $"{path}.then", depth + 1, isRequired: false, fields);
        if (node.ElseNode is not null) Walk(node.ElseNode, $"{path}.else", depth + 1, isRequired: false, fields);
    }

    private static string DescribeKind(SchemaNode node)
    {
        if (node.LocalDefinitionRef is not null) return "reference";
        if (node.ComponentReference is not null) return "component reference";
        if (node.Composition is not null) return DescribeComposition(node.Composition);

        return node.Kind?.ToString() ?? "unspecified";
    }

    private static string DescribeComposition(CompositionKind? kind) => kind switch
    {
        CompositionKind.OneOf => "oneOf",
        CompositionKind.AnyOf => "anyOf",
        CompositionKind.AllOf => "allOf",
        CompositionKind.Not => "not",
        _ => "composition",
    };

    private static List<string> DescribeConstraints(SchemaNode node)
    {
        var constraints = new List<string>();

        if (node.StringConstraints is { } sc)
        {
            if (sc.MinLength is not null) constraints.Add($"minLength: {sc.MinLength}");
            if (sc.MaxLength is not null) constraints.Add($"maxLength: {sc.MaxLength}");
            if (sc.Pattern is not null) constraints.Add($"pattern: {sc.Pattern}");
            if (sc.Format is not null) constraints.Add($"format: {sc.Format}");
        }

        if (node.NumericConstraints is { } nc)
        {
            if (nc.Minimum is not null) constraints.Add($"{(nc.ExclusiveMinimum ? "exclusive " : "")}minimum: {nc.Minimum}");
            if (nc.Maximum is not null) constraints.Add($"{(nc.ExclusiveMaximum ? "exclusive " : "")}maximum: {nc.Maximum}");
            if (nc.MultipleOf is not null) constraints.Add($"multipleOf: {nc.MultipleOf}");
        }

        if (node.ArrayConstraints is { } ac)
        {
            if (ac.MinItems is not null) constraints.Add($"minItems: {ac.MinItems}");
            if (ac.MaxItems is not null) constraints.Add($"maxItems: {ac.MaxItems}");
            if (ac.UniqueItems) constraints.Add("uniqueItems");
        }

        if (node.ObjectConstraints is { } oc)
        {
            if (oc.MinProperties is not null) constraints.Add($"minProperties: {oc.MinProperties}");
            if (oc.MaxProperties is not null) constraints.Add($"maxProperties: {oc.MaxProperties}");
            if (!oc.AdditionalPropertiesAllowed) constraints.Add("no additional properties");
        }

        return constraints;
    }
}
