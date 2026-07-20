namespace SchemaForge.Domain.Schemas;

public static class SchemaDiffComputer
{
    public static SchemaDiff Compute(
        SchemaNode sourceRoot, IReadOnlyList<LocalDefinition> sourceLocalDefinitions,
        SchemaNode targetRoot, IReadOnlyList<LocalDefinition> targetLocalDefinitions)
    {
        var sourceNodes = new Dictionary<string, SchemaNode>();
        var targetNodes = new Dictionary<string, SchemaNode>();

        Flatten(sourceRoot, "$", sourceNodes);
        Flatten(targetRoot, "$", targetNodes);
        foreach (var definition in sourceLocalDefinitions) Flatten(definition.RootNode, $"$defs.{definition.Name}", sourceNodes);
        foreach (var definition in targetLocalDefinitions) Flatten(definition.RootNode, $"$defs.{definition.Name}", targetNodes);

        var added = CollapseToShallowest([.. targetNodes.Keys.Except(sourceNodes.Keys)]);
        var removed = CollapseToShallowest([.. sourceNodes.Keys.Except(targetNodes.Keys)]);

        var changed = new List<SchemaDiffChange>();
        foreach (var path in sourceNodes.Keys.Intersect(targetNodes.Keys).OrderBy(p => p, StringComparer.Ordinal))
        {
            var changes = DescribeChanges(sourceNodes[path], targetNodes[path]);
            if (changes.Count > 0)
            {
                changed.Add(new SchemaDiffChange(path, changes));
            }
        }

        return new SchemaDiff(added, removed, changed);
    }

    private static List<string> DescribeChanges(SchemaNode source, SchemaNode target)
    {
        var changes = new List<string>();

        if (source.Kind != target.Kind)
        {
            changes.Add($"kind changed from {Describe(source.Kind)} to {Describe(target.Kind)}");
        }

        if (source.IsNullable != target.IsNullable)
        {
            changes.Add($"nullable changed from {source.IsNullable} to {target.IsNullable}");
        }

        if (source.IsRequiredByParent != target.IsRequiredByParent)
        {
            changes.Add($"required changed from {source.IsRequiredByParent} to {target.IsRequiredByParent}");
        }

        // Every constraint/reference type here is a record, so != is structural (value)
        // equality, not reference equality - exactly what a field-by-field comparison would do,
        // without writing one out per type.
        if (source.StringConstraints != target.StringConstraints) changes.Add("string constraints changed");
        if (source.NumericConstraints != target.NumericConstraints) changes.Add("numeric constraints changed");
        if (source.ArrayConstraints != target.ArrayConstraints) changes.Add("array constraints changed");
        if (source.ObjectConstraints != target.ObjectConstraints) changes.Add("object constraints changed");
        if (source.Composition != target.Composition) changes.Add("composition changed");
        if (source.ComponentReference != target.ComponentReference) changes.Add("component reference changed");
        if (source.LocalDefinitionRef != target.LocalDefinitionRef) changes.Add("local definition reference changed");

        return changes;
    }

    private static void Flatten(SchemaNode node, string path, Dictionary<string, SchemaNode> into)
    {
        into[path] = node;

        foreach (var child in node.Properties)
        {
            Flatten(child, $"{path}.{child.PropertyName}", into);
        }

        for (var i = 0; i < node.PrefixItems.Count; i++)
        {
            Flatten(node.PrefixItems[i], $"{path}[{i}]", into);
        }

        if (node.ItemsNode is not null)
        {
            Flatten(node.ItemsNode, $"{path}[]", into);
        }

        for (var i = 0; i < node.CompositionBranches.Count; i++)
        {
            Flatten(node.CompositionBranches[i], $"{path}.{Describe(node.Composition)}[{i}]", into);
        }

        if (node.IfNode is not null) Flatten(node.IfNode, $"{path}.if", into);
        if (node.ThenNode is not null) Flatten(node.ThenNode, $"{path}.then", into);
        if (node.ElseNode is not null) Flatten(node.ElseNode, $"{path}.else", into);
    }

    // A newly added (or removed) subtree flattens into one entry per descendant node - without
    // this, adding one new object with five properties would report six additions instead of
    // one. Keeps only the shallowest path in each family of ancestor/descendant paths.
    private static List<string> CollapseToShallowest(List<string> paths)
    {
        var sorted = paths.OrderBy(p => p.Length).ToList();
        var result = new List<string>();

        foreach (var path in sorted)
        {
            var isDescendantOfExisting = result.Any(existing => path.StartsWith(existing + ".", StringComparison.Ordinal)
                || path.StartsWith(existing + "[", StringComparison.Ordinal));
            if (!isDescendantOfExisting)
            {
                result.Add(path);
            }
        }

        return [.. result.OrderBy(p => p, StringComparer.Ordinal)];
    }

    private static string Describe(NodeKind? kind) => kind?.ToString() ?? "unspecified";

    private static string Describe(CompositionKind? kind) => kind switch
    {
        CompositionKind.OneOf => "oneOf",
        CompositionKind.AnyOf => "anyOf",
        CompositionKind.AllOf => "allOf",
        CompositionKind.Not => "not",
        _ => "composition",
    };
}
