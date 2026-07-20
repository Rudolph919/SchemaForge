using System.Text.Json;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Generation;

// The reverse of JsonSchemaNodeWriter (Step 4 §4.4): "type": [X, "null"] becomes IsNullable, the
// parent's "required" array becomes each named child's IsRequiredByParent. $defs entries become
// LocalDefinitions, resolved before the root schema is walked so a $ref anywhere in the document
// (including inside $defs itself, for mutual/self-recursion) can be matched against a real id.
public sealed class JsonSchemaImporter : IJsonSchemaImporter
{
    public Result Import(SchemaVersion version, JsonElement schemaDocument)
    {
        var localDefinitionIds = new Dictionary<string, Guid>();

        if (schemaDocument.TryGetProperty("$defs", out var defsElement) && defsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var def in defsElement.EnumerateObject())
            {
                var (kind, _) = ParseType(def.Value);
                var addResult = version.AddLocalDefinition(def.Name, kind);
                if (addResult.IsFailure) return Result.Failure(addResult.Error);
                localDefinitionIds[def.Name] = addResult.Value;
            }

            foreach (var def in defsElement.EnumerateObject())
            {
                var localDefinition = version.LocalDefinitions.Single(d => d.Name == def.Name);
                var result = PopulateNode(version, localDefinition.RootNode.Id, def.Value, isRequiredByParent: false, localDefinitionIds);
                if (result.IsFailure) return result;
            }
        }

        return PopulateNode(version, version.RootNode.Id, schemaDocument, isRequiredByParent: false, localDefinitionIds);
    }

    private static Result PopulateNode(
        SchemaVersion version, Guid nodeId, JsonElement schema, bool isRequiredByParent, Dictionary<string, Guid> localDefinitionIds)
    {
        if (schema.TryGetProperty("$ref", out var refElement))
        {
            return PopulateRef(version, nodeId, refElement.GetString(), isRequiredByParent, localDefinitionIds);
        }

        var (kind, isNullable) = ParseType(schema);

        var content = new SchemaNodeContent(
            kind,
            schema.TryGetProperty("description", out var descEl) ? descEl.GetString() : null,
            Notes: null,
            isNullable,
            isRequiredByParent,
            ParseLiteralArray(schema, "examples"),
            schema.TryGetProperty("default", out var defEl) ? ParseLiteral(defEl) : null,
            schema.TryGetProperty("enum", out var enumEl) ? [.. enumEl.EnumerateArray().Select(ParseLiteral)] : null,
            schema.TryGetProperty("const", out var constEl) ? ParseLiteral(constEl) : null,
            kind == NodeKind.Object ? ParseObjectConstraints(schema) : null,
            kind == NodeKind.Array ? ParseArrayConstraints(schema) : null,
            kind == NodeKind.String ? ParseStringConstraints(schema) : null,
            kind is NodeKind.Number or NodeKind.Integer ? ParseNumericConstraints(schema) : null,
            ParseDependentRequired(schema),
            ParseCompositionKind(schema),
            ComponentReference: null,
            LocalDefinitionRef: null);

        var updateResult = version.UpdateNode(nodeId, content);
        if (updateResult.IsFailure) return updateResult;

        if (kind == NodeKind.Object && schema.TryGetProperty("properties", out var propertiesEl))
        {
            var required = schema.TryGetProperty("required", out var reqEl)
                ? reqEl.EnumerateArray().Select(e => e.GetString()!).ToHashSet()
                : [];

            foreach (var property in propertiesEl.EnumerateObject())
            {
                var (childKind, _) = ParseType(property.Value);
                var addResult = version.AddObjectProperty(nodeId, property.Name, childKind);
                if (addResult.IsFailure) return Result.Failure(addResult.Error);

                var childResult = PopulateNode(
                    version, addResult.Value, property.Value, required.Contains(property.Name), localDefinitionIds);
                if (childResult.IsFailure) return childResult;
            }
        }

        if (kind == NodeKind.Array)
        {
            if (schema.TryGetProperty("prefixItems", out var prefixItemsEl))
            {
                foreach (var item in prefixItemsEl.EnumerateArray())
                {
                    var (itemKind, _) = ParseType(item);
                    var addResult = version.AddArrayPrefixItem(nodeId, itemKind);
                    if (addResult.IsFailure) return Result.Failure(addResult.Error);

                    var itemResult = PopulateNode(version, addResult.Value, item, isRequiredByParent: false, localDefinitionIds);
                    if (itemResult.IsFailure) return itemResult;
                }
            }

            if (schema.TryGetProperty("items", out var itemsEl))
            {
                var (itemsKind, _) = ParseType(itemsEl);
                var setResult = version.SetArrayItemsNode(nodeId, itemsKind);
                if (setResult.IsFailure) return Result.Failure(setResult.Error);

                var itemsResult = PopulateNode(version, setResult.Value, itemsEl, isRequiredByParent: false, localDefinitionIds);
                if (itemsResult.IsFailure) return itemsResult;
            }
        }

        var compositionResult = PopulateComposition(version, nodeId, schema, localDefinitionIds);
        if (compositionResult.IsFailure) return compositionResult;

        return PopulateConditional(version, nodeId, schema, localDefinitionIds);
    }

    private static Result PopulateRef(
        SchemaVersion version, Guid nodeId, string? refValue, bool isRequiredByParent, Dictionary<string, Guid> localDefinitionIds)
    {
        const string prefix = "#/$defs/";
        Guid? resolvedId = refValue is not null && refValue.StartsWith(prefix, StringComparison.Ordinal)
            && localDefinitionIds.TryGetValue(refValue[prefix.Length..], out var id)
                ? id
                : null;

        // An unresolvable $ref (external URI, or a component reference this importer doesn't
        // attempt to re-resolve back into a ComponentReference) is imported as an untyped node
        // with a note, rather than failing the whole import over one field the importer can't
        // fully round-trip.
        var content = SchemaNodeContent.Empty(null) with
        {
            IsRequiredByParent = isRequiredByParent,
            LocalDefinitionRef = resolvedId,
            Notes = resolvedId is null ? $"Unresolved $ref: {refValue}" : null,
        };

        return version.UpdateNode(nodeId, content);
    }

    private static Result PopulateComposition(
        SchemaVersion version, Guid nodeId, JsonElement schema, Dictionary<string, Guid> localDefinitionIds)
    {
        foreach (var (keyword, kind) in new[]
        {
            ("oneOf", CompositionKind.OneOf), ("anyOf", CompositionKind.AnyOf), ("allOf", CompositionKind.AllOf),
        })
        {
            if (!schema.TryGetProperty(keyword, out var branchesEl)) continue;

            foreach (var branch in branchesEl.EnumerateArray())
            {
                var (branchKind, _) = ParseType(branch);
                var addResult = version.AddCompositionBranch(nodeId, branchKind);
                if (addResult.IsFailure) return Result.Failure(addResult.Error);

                var branchResult = PopulateNode(version, addResult.Value, branch, isRequiredByParent: false, localDefinitionIds);
                if (branchResult.IsFailure) return branchResult;
            }

            _ = kind; // composition itself was already set as part of the node's own content above
        }

        if (schema.TryGetProperty("not", out var notEl))
        {
            var (notKind, _) = ParseType(notEl);
            var addResult = version.AddCompositionBranch(nodeId, notKind);
            if (addResult.IsFailure) return Result.Failure(addResult.Error);

            return PopulateNode(version, addResult.Value, notEl, isRequiredByParent: false, localDefinitionIds);
        }

        return Result.Success();
    }

    private static Result PopulateConditional(
        SchemaVersion version, Guid nodeId, JsonElement schema, Dictionary<string, Guid> localDefinitionIds)
    {
        foreach (var (keyword, slot) in new[]
        {
            ("if", ConditionalSlot.If), ("then", ConditionalSlot.Then), ("else", ConditionalSlot.Else),
        })
        {
            if (!schema.TryGetProperty(keyword, out var slotEl)) continue;

            var (slotKind, _) = ParseType(slotEl);
            var setResult = version.SetConditionalNode(nodeId, slot, slotKind);
            if (setResult.IsFailure) return Result.Failure(setResult.Error);

            var slotResult = PopulateNode(version, setResult.Value, slotEl, isRequiredByParent: false, localDefinitionIds);
            if (slotResult.IsFailure) return slotResult;
        }

        return Result.Success();
    }

    private static (NodeKind? Kind, bool IsNullable) ParseType(JsonElement schema)
    {
        if (!schema.TryGetProperty("type", out var typeEl)) return (null, false);

        if (typeEl.ValueKind == JsonValueKind.String)
        {
            return (MapType(typeEl.GetString()!), false);
        }

        if (typeEl.ValueKind == JsonValueKind.Array)
        {
            var types = typeEl.EnumerateArray().Select(e => e.GetString()!).ToList();
            var nonNull = types.FirstOrDefault(t => t != "null");
            return (nonNull is null ? null : MapType(nonNull), types.Contains("null"));
        }

        return (null, false);
    }

    private static NodeKind? MapType(string type) => type switch
    {
        "object" => NodeKind.Object,
        "array" => NodeKind.Array,
        "string" => NodeKind.String,
        "number" => NodeKind.Number,
        "integer" => NodeKind.Integer,
        "boolean" => NodeKind.Boolean,
        "null" => NodeKind.Null,
        _ => null,
    };

    private static CompositionKind? ParseCompositionKind(JsonElement schema)
    {
        if (schema.TryGetProperty("oneOf", out _)) return CompositionKind.OneOf;
        if (schema.TryGetProperty("anyOf", out _)) return CompositionKind.AnyOf;
        if (schema.TryGetProperty("allOf", out _)) return CompositionKind.AllOf;
        if (schema.TryGetProperty("not", out _)) return CompositionKind.Not;
        return null;
    }

    private static ObjectConstraints? ParseObjectConstraints(JsonElement schema)
    {
        var minProperties = schema.TryGetProperty("minProperties", out var minEl) ? minEl.GetInt32() : (int?)null;
        var maxProperties = schema.TryGetProperty("maxProperties", out var maxEl) ? maxEl.GetInt32() : (int?)null;
        var additionalPropertiesAllowed = !(schema.TryGetProperty("additionalProperties", out var apEl)
            && apEl.ValueKind == JsonValueKind.False);

        return new ObjectConstraints(minProperties, maxProperties, additionalPropertiesAllowed);
    }

    private static ArrayConstraints? ParseArrayConstraints(JsonElement schema)
    {
        var minItems = schema.TryGetProperty("minItems", out var minEl) ? minEl.GetInt32() : (int?)null;
        var maxItems = schema.TryGetProperty("maxItems", out var maxEl) ? maxEl.GetInt32() : (int?)null;
        var uniqueItems = schema.TryGetProperty("uniqueItems", out var uniqueEl) && uniqueEl.ValueKind == JsonValueKind.True;

        return new ArrayConstraints(minItems, maxItems, uniqueItems);
    }

    private static StringConstraints? ParseStringConstraints(JsonElement schema)
    {
        var minLength = schema.TryGetProperty("minLength", out var minEl) ? minEl.GetInt32() : (int?)null;
        var maxLength = schema.TryGetProperty("maxLength", out var maxEl) ? maxEl.GetInt32() : (int?)null;
        var pattern = schema.TryGetProperty("pattern", out var patternEl) ? patternEl.GetString() : null;
        var (format, customValue) = schema.TryGetProperty("format", out var formatEl)
            ? MapFormat(formatEl.GetString()!)
            : (null, null);

        return new StringConstraints(minLength, maxLength, pattern, format, customValue);
    }

    private static NumericConstraints? ParseNumericConstraints(JsonElement schema)
    {
        decimal? minimum = null;
        var exclusiveMinimum = false;
        if (schema.TryGetProperty("exclusiveMinimum", out var exMinEl))
        {
            minimum = exMinEl.GetDecimal();
            exclusiveMinimum = true;
        }
        else if (schema.TryGetProperty("minimum", out var minEl))
        {
            minimum = minEl.GetDecimal();
        }

        decimal? maximum = null;
        var exclusiveMaximum = false;
        if (schema.TryGetProperty("exclusiveMaximum", out var exMaxEl))
        {
            maximum = exMaxEl.GetDecimal();
            exclusiveMaximum = true;
        }
        else if (schema.TryGetProperty("maximum", out var maxEl))
        {
            maximum = maxEl.GetDecimal();
        }

        var multipleOf = schema.TryGetProperty("multipleOf", out var mulEl) ? mulEl.GetDecimal() : (decimal?)null;

        return new NumericConstraints(minimum, maximum, exclusiveMinimum, exclusiveMaximum, multipleOf);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>>? ParseDependentRequired(JsonElement schema)
    {
        if (!schema.TryGetProperty("dependentRequired", out var el) || el.ValueKind != JsonValueKind.Object) return null;

        var result = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var property in el.EnumerateObject())
        {
            result[property.Name] = [.. property.Value.EnumerateArray().Select(e => e.GetString()!)];
        }

        return result;
    }

    private static (SchemaFormat? Format, string? CustomValue) MapFormat(string format) => format switch
    {
        "date" => (SchemaFormat.Date, null),
        "date-time" => (SchemaFormat.DateTime, null),
        "time" => (SchemaFormat.Time, null),
        "email" => (SchemaFormat.Email, null),
        "hostname" => (SchemaFormat.Hostname, null),
        "ipv4" => (SchemaFormat.Ipv4, null),
        "ipv6" => (SchemaFormat.Ipv6, null),
        "uri" => (SchemaFormat.Uri, null),
        "uri-reference" => (SchemaFormat.UriReference, null),
        "uuid" => (SchemaFormat.Uuid, null),
        _ => (SchemaFormat.Custom, format),
    };

    private static IReadOnlyList<JsonLiteral> ParseLiteralArray(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var el) && el.ValueKind == JsonValueKind.Array
            ? [.. el.EnumerateArray().Select(ParseLiteral)]
            : [];

    private static JsonLiteral ParseLiteral(JsonElement element) => JsonLiteral.FromRawJson(element.GetRawText());
}
