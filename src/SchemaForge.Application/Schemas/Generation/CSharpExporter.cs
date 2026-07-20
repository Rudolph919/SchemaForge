using System.Globalization;
using System.Text;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

// Unlike TypeScript's structural typing, C# records need a name per nested object type - each
// nested object gets its own record, named after the property that holds it (capitalized,
// deduplicated on collision), hoisted alongside the root "Schema" record rather than nested
// inline. Composition (oneOf/anyOf/allOf/not) has no direct C# record equivalent and is exported
// as `object` with a comment rather than attempting a discriminated-union encoding.
public sealed class CSharpExporter : ISchemaExporter
{
    public string FormatKey => "csharp";

    public Task<string> ExportAsync(SchemaVersion version, CancellationToken cancellationToken)
    {
        var records = new List<string>();
        var usedNames = new HashSet<string> { "Schema" };
        WriteRecord(version.RootNode, "Schema", records, usedNames);

        var sb = new StringBuilder();
        sb.Append("using System;\nusing System.Collections.Generic;\n\n");
        sb.Append(string.Join("\n\n", records));
        sb.Append('\n');
        return Task.FromResult(sb.ToString());
    }

    private static void WriteRecord(SchemaNode node, string name, List<string> records, HashSet<string> usedNames)
    {
        var properties = new List<string>();

        foreach (var child in node.Properties.OrderBy(p => p.Order))
        {
            var propertyName = Capitalize(child.PropertyName!);
            var csType = WriteType(child, UniqueName(propertyName, usedNames), records, usedNames);
            // `?` is valid on both value types (Nullable<T>) and reference types (nullable
            // annotation) in modern C#, so no value/reference-type branching is needed here.
            var nullableSuffix = child.IsRequiredByParent ? "" : "?";
            var defaultClause = child.IsRequiredByParent ? "" : " = default";
            properties.Add($"{csType}{nullableSuffix} {propertyName}{defaultClause}");
        }

        records.Add($"public sealed record {name}({string.Join(", ", properties)});");
    }

    private static string WriteType(SchemaNode node, string proposedName, List<string> records, HashSet<string> usedNames)
    {
        if (node.Composition is not null)
        {
            return "object /* composition: no direct C# record equivalent */";
        }

        return node.Kind switch
        {
            NodeKind.Object => WriteNestedRecord(node, proposedName, records, usedNames),
            NodeKind.Array => $"IReadOnlyList<{(node.ItemsNode is not null ? WriteType(node.ItemsNode, proposedName + "Item", records, usedNames) : "object")}>",
            NodeKind.String => "string",
            NodeKind.Number => "decimal",
            NodeKind.Integer => "int",
            NodeKind.Boolean => "bool",
            NodeKind.Null => "object",
            _ => "object",
        };
    }

    private static string WriteNestedRecord(SchemaNode node, string name, List<string> records, HashSet<string> usedNames)
    {
        WriteRecord(node, name, records, usedNames);
        return name;
    }

    private static string UniqueName(string proposed, HashSet<string> usedNames)
    {
        var candidate = proposed;
        var suffix = 1;
        while (!usedNames.Add(candidate))
        {
            candidate = proposed + (++suffix).ToString(CultureInfo.InvariantCulture);
        }

        return candidate;
    }

    private static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
