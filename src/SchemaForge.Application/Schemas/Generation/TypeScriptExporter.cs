using System.Text;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

// Every nested object type is written inline (TypeScript's structural object-literal types make
// this natural, unlike C#'s exporter which has to name and hoist a record per nested object) -
// keeps the output to a single top-level interface rather than inventing names for every nested
// shape. "not" composition has no direct TypeScript equivalent and is exported as `unknown` with
// a comment rather than attempting a real negated-type encoding.
public sealed class TypeScriptExporter : ISchemaExporter
{
    public string FormatKey => "typescript";

    public Task<string> ExportAsync(SchemaVersion version, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.Append("export type Schema = ").Append(WriteType(version.RootNode, 0)).Append(';').Append('\n');
        return Task.FromResult(sb.ToString());
    }

    private static string WriteType(SchemaNode node, int indent)
    {
        string baseType;

        if (node.ConstValue is not null)
        {
            baseType = node.ConstValue.RawJson;
        }
        else if (node.AllowedValues is { Count: > 0 })
        {
            baseType = string.Join(" | ", node.AllowedValues.Select(v => v.RawJson));
        }
        else if (node.Composition is not null)
        {
            baseType = WriteCompositionType(node, indent);
        }
        else
        {
            baseType = node.Kind switch
            {
                NodeKind.Object => WriteObjectType(node, indent),
                NodeKind.Array => WriteArrayType(node, indent),
                NodeKind.String => "string",
                NodeKind.Number or NodeKind.Integer => "number",
                NodeKind.Boolean => "boolean",
                NodeKind.Null => "null",
                _ => "unknown",
            };
        }

        return node.IsNullable ? $"{baseType} | null" : baseType;
    }

    private static string WriteObjectType(SchemaNode node, int indent)
    {
        if (node.Properties.Count == 0) return "Record<string, unknown>";

        var pad = new string(' ', (indent + 1) * 2);
        var closePad = new string(' ', indent * 2);
        var sb = new StringBuilder("{\n");

        foreach (var child in node.Properties.OrderBy(p => p.Order))
        {
            var optional = child.IsRequiredByParent ? "" : "?";
            sb.Append(pad).Append(child.PropertyName).Append(optional).Append(": ")
                .Append(WriteType(child, indent + 1)).Append(";\n");
        }

        sb.Append(closePad).Append('}');
        return sb.ToString();
    }

    private static string WriteArrayType(SchemaNode node, int indent)
    {
        if (node.ItemsNode is not null)
        {
            return $"({WriteType(node.ItemsNode, indent)})[]";
        }

        if (node.PrefixItems.Count > 0)
        {
            return $"[{string.Join(", ", node.PrefixItems.OrderBy(i => i.Order).Select(i => WriteType(i, indent)))}]";
        }

        return "unknown[]";
    }

    private static string WriteCompositionType(SchemaNode node, int indent)
    {
        if (node.Composition == CompositionKind.Not)
        {
            return "unknown /* not: negated type has no direct TypeScript equivalent */";
        }

        var separator = node.Composition == CompositionKind.AllOf ? " & " : " | ";
        return string.Join(separator, node.CompositionBranches.OrderBy(b => b.Order).Select(b => $"({WriteType(b, indent)})"));
    }
}
