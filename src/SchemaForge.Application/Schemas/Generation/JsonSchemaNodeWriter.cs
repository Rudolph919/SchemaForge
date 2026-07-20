using System.Text.Json.Nodes;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

// Shared by JsonSchemaExporter and OpenApiExporter (OpenAPI 3.1's schema object is JSON-Schema-
// compatible) - the node -> JSON Schema translation is one piece of logic, not duplicated per
// consumer. Implements the authoring-model -> wire-format translations from Step 4 §4.4/§4.5:
// IsNullable becomes a "type": [...,"null"] array entry rather than a separate keyword (2020-12
// has no native "nullable"), and each child's IsRequiredByParent flag gets aggregated into the
// parent object's "required" array on the way out.
internal static class JsonSchemaNodeWriter
{
    public static JsonObject WriteVersion(SchemaVersion version)
    {
        var localDefinitionNames = version.LocalDefinitions.ToDictionary(d => d.Id, d => d.Name);

        var root = WriteNode(version.RootNode, localDefinitionNames);
        root.Insert(0, "$schema", "https://json-schema.org/draft/2020-12/schema");

        if (version.LocalDefinitions.Count > 0)
        {
            var defs = new JsonObject();
            foreach (var definition in version.LocalDefinitions)
            {
                defs[definition.Name] = WriteNode(definition.RootNode, localDefinitionNames);
            }

            root["$defs"] = defs;
        }

        return root;
    }

    // localDefinitionNames resolves a LocalDefinitionRef id to its "#/$defs/{name}" target -
    // omit it (standalone WriteNode calls that aren't going through WriteVersion) and a
    // LocalDefinitionRef degrades to a $comment marker instead of a broken/guessed $ref.
    public static JsonObject WriteNode(SchemaNode node, IReadOnlyDictionary<Guid, string>? localDefinitionNames = null)
    {
        var obj = new JsonObject();

        if (node.Description is not null) obj["description"] = node.Description;

        if (node.Kind is not null)
        {
            var typeName = MapKind(node.Kind.Value);
            obj["type"] = node.IsNullable ? new JsonArray(typeName, "null") : typeName;
        }

        if (node.ConstValue is not null) obj["const"] = ParseLiteral(node.ConstValue.RawJson);
        if (node.DefaultValue is not null) obj["default"] = ParseLiteral(node.DefaultValue.RawJson);
        if (node.AllowedValues is { Count: > 0 })
        {
            obj["enum"] = new JsonArray([.. node.AllowedValues.Select(v => ParseLiteral(v.RawJson))]);
        }

        if (node.Examples.Count > 0)
        {
            obj["examples"] = new JsonArray([.. node.Examples.Select(v => ParseLiteral(v.RawJson))]);
        }

        switch (node.Kind)
        {
            case NodeKind.Object:
                WriteObjectKeywords(node, obj, localDefinitionNames);
                break;
            case NodeKind.Array:
                WriteArrayKeywords(node, obj, localDefinitionNames);
                break;
            case NodeKind.String:
                WriteStringKeywords(node, obj);
                break;
            case NodeKind.Number or NodeKind.Integer:
                WriteNumericKeywords(node, obj);
                break;
        }

        if (node.Composition is not null)
        {
            var branches = new JsonArray(
                [.. node.CompositionBranches.OrderBy(b => b.Order).Select(b => (JsonNode)WriteNode(b, localDefinitionNames))]);
            obj[CompositionKeyword(node.Composition.Value)] = node.Composition == CompositionKind.Not
                ? node.CompositionBranches.Count > 0 ? WriteNode(node.CompositionBranches[0], localDefinitionNames) : new JsonObject()
                : branches;
        }

        if (node.IfNode is not null) obj["if"] = WriteNode(node.IfNode, localDefinitionNames);
        if (node.ThenNode is not null) obj["then"] = WriteNode(node.ThenNode, localDefinitionNames);
        if (node.ElseNode is not null) obj["else"] = WriteNode(node.ElseNode, localDefinitionNames);

        if (node.LocalDefinitionRef is { } localDefId)
        {
            obj["$ref"] = localDefinitionNames is not null && localDefinitionNames.TryGetValue(localDefId, out var name)
                ? $"#/$defs/{name}"
                : $"#/$defs/{localDefId}"; // Standalone WriteNode call with no version context to resolve the name from.
        }

        if (node.ComponentReference is not null)
        {
            // Cross-schema reference to an Organization-scoped ComponentVersion (Step 2 §3) -
            // resolving and inlining the referenced component's tree would need repository
            // access this pure node-writer deliberately doesn't have. Emits a $ref by id rather
            // than a real resolvable URI, since SchemaForge has no public schema-hosting endpoint
            // serving components yet - a known simplification, not a spec-compliant $ref today.
            obj["$ref"] = $"#/components/{node.ComponentReference.ComponentVersionId}";
        }

        return obj;
    }

    private static void WriteObjectKeywords(SchemaNode node, JsonObject obj, IReadOnlyDictionary<Guid, string>? localDefinitionNames)
    {
        if (node.Properties.Count > 0)
        {
            var properties = new JsonObject();
            var required = new JsonArray();
            foreach (var child in node.Properties.OrderBy(p => p.Order))
            {
                properties[child.PropertyName!] = WriteNode(child, localDefinitionNames);
                if (child.IsRequiredByParent) required.Add(child.PropertyName);
            }

            obj["properties"] = properties;
            if (required.Count > 0) obj["required"] = required;
        }

        if (node.DependentRequired is { Count: > 0 })
        {
            var dependentRequired = new JsonObject();
            foreach (var (key, values) in node.DependentRequired)
            {
                dependentRequired[key] = new JsonArray([.. values.Select(v => (JsonNode)v)]);
            }

            obj["dependentRequired"] = dependentRequired;
        }

        if (node.ObjectConstraints is { } oc)
        {
            if (oc.MinProperties is not null) obj["minProperties"] = oc.MinProperties;
            if (oc.MaxProperties is not null) obj["maxProperties"] = oc.MaxProperties;
            if (!oc.AdditionalPropertiesAllowed) obj["additionalProperties"] = false;
        }
    }

    private static void WriteArrayKeywords(SchemaNode node, JsonObject obj, IReadOnlyDictionary<Guid, string>? localDefinitionNames)
    {
        if (node.PrefixItems.Count > 0)
        {
            obj["prefixItems"] = new JsonArray(
                [.. node.PrefixItems.OrderBy(i => i.Order).Select(i => (JsonNode)WriteNode(i, localDefinitionNames))]);
        }

        if (node.ItemsNode is not null) obj["items"] = WriteNode(node.ItemsNode, localDefinitionNames);

        if (node.ArrayConstraints is { } ac)
        {
            if (ac.MinItems is not null) obj["minItems"] = ac.MinItems;
            if (ac.MaxItems is not null) obj["maxItems"] = ac.MaxItems;
            if (ac.UniqueItems) obj["uniqueItems"] = true;
        }
    }

    private static void WriteStringKeywords(SchemaNode node, JsonObject obj)
    {
        if (node.StringConstraints is not { } sc) return;

        if (sc.MinLength is not null) obj["minLength"] = sc.MinLength;
        if (sc.MaxLength is not null) obj["maxLength"] = sc.MaxLength;
        if (sc.Pattern is not null) obj["pattern"] = sc.Pattern;
        if (sc.Format is not null) obj["format"] = MapFormat(sc.Format.Value, sc.CustomFormatValue);
    }

    private static void WriteNumericKeywords(SchemaNode node, JsonObject obj)
    {
        if (node.NumericConstraints is not { } nc) return;

        if (nc.Minimum is not null) obj[nc.ExclusiveMinimum ? "exclusiveMinimum" : "minimum"] = nc.Minimum;
        if (nc.Maximum is not null) obj[nc.ExclusiveMaximum ? "exclusiveMaximum" : "maximum"] = nc.Maximum;
        if (nc.MultipleOf is not null) obj["multipleOf"] = nc.MultipleOf;
    }

    private static JsonNode? ParseLiteral(string rawJson) => JsonNode.Parse(rawJson);

    private static string MapKind(NodeKind kind) => kind switch
    {
        NodeKind.Object => "object",
        NodeKind.Array => "array",
        NodeKind.String => "string",
        NodeKind.Number => "number",
        NodeKind.Integer => "integer",
        NodeKind.Boolean => "boolean",
        NodeKind.Null => "null",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown node kind."),
    };

    private static string CompositionKeyword(CompositionKind kind) => kind switch
    {
        CompositionKind.OneOf => "oneOf",
        CompositionKind.AnyOf => "anyOf",
        CompositionKind.AllOf => "allOf",
        CompositionKind.Not => "not",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown composition kind."),
    };

    private static string MapFormat(SchemaFormat format, string? customValue) => format switch
    {
        SchemaFormat.Date => "date",
        SchemaFormat.DateTime => "date-time",
        SchemaFormat.Time => "time",
        SchemaFormat.Email => "email",
        SchemaFormat.Hostname => "hostname",
        SchemaFormat.Ipv4 => "ipv4",
        SchemaFormat.Ipv6 => "ipv6",
        SchemaFormat.Uri => "uri",
        SchemaFormat.UriReference => "uri-reference",
        SchemaFormat.Uuid => "uuid",
        SchemaFormat.Custom => customValue ?? "custom",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown schema format."),
    };
}
