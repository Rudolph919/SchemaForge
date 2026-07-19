using System.Text.Json;
using System.Text.RegularExpressions;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas.Validation;

public sealed class SchemaValidator : ISchemaValidator
{
    public IReadOnlyList<ValidationError> Validate(
        SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, JsonElement payload)
    {
        var errors = new List<ValidationError>();
        ValidateNode(rootNode, payload, JsonPath.Root, localDefinitions, errors);
        return errors;
    }

    // Isolated variant used by composition (oneOf/anyOf/allOf/not) to try a branch without
    // committing its errors to the caller's list until the branch's pass/fail outcome is known.
    private List<ValidationError> ValidateNodeIsolated(
        SchemaNode node, JsonElement value, JsonPath path, IReadOnlyList<LocalDefinition> localDefinitions)
    {
        var errors = new List<ValidationError>();
        ValidateNode(node, value, path, localDefinitions, errors);
        return errors;
    }

    private void ValidateNode(
        SchemaNode node, JsonElement value, JsonPath path, IReadOnlyList<LocalDefinition> localDefinitions,
        List<ValidationError> errors)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            // Null is acceptable if the node explicitly allows it, or has no Kind constraint at
            // all (a composition-only node has nothing of its own to say "null is wrong").
            if (!node.IsNullable && node.Kind is not null)
            {
                errors.Add(new ValidationError(path, "type.null-not-allowed", "Value must not be null.", ErrorSeverity.Error));
            }

            return;
        }

        if (node.ConstValue is not null && !JsonValuesEqual(value, node.ConstValue.AsJsonElement()))
        {
            errors.Add(new ValidationError(path, "const.mismatch", "Value does not match the required constant.", ErrorSeverity.Error));
        }

        if (node.AllowedValues is { Count: > 0 } allowedValues
            && !allowedValues.Any(allowed => JsonValuesEqual(value, allowed.AsJsonElement())))
        {
            errors.Add(new ValidationError(path, "enum.mismatch", "Value is not one of the allowed values.", ErrorSeverity.Error));
        }

        if (node.Kind is not null)
        {
            ValidateKind(node, value, path, localDefinitions, errors);
        }

        if (node.Composition is not null)
        {
            ValidateComposition(node, value, path, localDefinitions, errors);
        }

        if (node.IfNode is not null)
        {
            ValidateConditional(node, value, path, localDefinitions, errors);
        }

        if (node.LocalDefinitionRef is { } localDefinitionId)
        {
            var definition = localDefinitions.FirstOrDefault(d => d.Id == localDefinitionId);
            if (definition is not null)
            {
                ValidateNode(definition.RootNode, value, path, localDefinitions, errors);
            }
        }

        // ComponentReference isn't resolved here - ComponentDefinition/ComponentVersion don't
        // exist as queryable aggregates yet (Phase 3), same honest gap as
        // PublishSchemaVersionHandler's component-reference check.
    }

    private void ValidateKind(
        SchemaNode node, JsonElement value, JsonPath path, IReadOnlyList<LocalDefinition> localDefinitions,
        List<ValidationError> errors)
    {
        switch (node.Kind!.Value)
        {
            case NodeKind.Object:
                ValidateObject(node, value, path, localDefinitions, errors);
                break;
            case NodeKind.Array:
                ValidateArray(node, value, path, localDefinitions, errors);
                break;
            case NodeKind.String:
                ValidateString(node, value, path, errors);
                break;
            case NodeKind.Number:
                ValidateNumeric(node, value, path, errors, allowNonInteger: true);
                break;
            case NodeKind.Integer:
                ValidateNumeric(node, value, path, errors, allowNonInteger: false);
                break;
            case NodeKind.Boolean:
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    errors.Add(TypeMismatch(path, "boolean"));
                }
                break;
            case NodeKind.Null:
                // A non-null value already fell through the null check above - Kind.Null with a
                // non-null payload value is always a mismatch.
                errors.Add(TypeMismatch(path, "null"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(node), node.Kind, "Unknown node kind.");
        }
    }

    private void ValidateObject(
        SchemaNode node, JsonElement value, JsonPath path, IReadOnlyList<LocalDefinition> localDefinitions,
        List<ValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            errors.Add(TypeMismatch(path, "object"));
            return;
        }

        var seenProperties = new HashSet<string>();

        foreach (var property in value.EnumerateObject())
        {
            seenProperties.Add(property.Name);
            var childNode = node.Properties.FirstOrDefault(p => p.PropertyName == property.Name);

            if (childNode is not null)
            {
                ValidateNode(childNode, property.Value, path.AppendProperty(property.Name), localDefinitions, errors);
            }
            else if (node.ObjectConstraints?.AdditionalPropertiesAllowed == false)
            {
                errors.Add(new ValidationError(
                    path.AppendProperty(property.Name), "object.additional-property-not-allowed",
                    $"Property '{property.Name}' is not defined on this schema and additional properties aren't allowed.",
                    ErrorSeverity.Error));
            }
        }

        foreach (var requiredChild in node.Properties.Where(p => p.IsRequiredByParent))
        {
            if (!seenProperties.Contains(requiredChild.PropertyName!))
            {
                errors.Add(new ValidationError(
                    path.AppendProperty(requiredChild.PropertyName!), "object.required-property-missing",
                    $"Required property '{requiredChild.PropertyName}' is missing.", ErrorSeverity.Error));
            }
        }

        if (node.DependentRequired is not null)
        {
            foreach (var (triggerProperty, dependents) in node.DependentRequired)
            {
                if (!seenProperties.Contains(triggerProperty)) continue;

                foreach (var dependent in dependents.Where(d => !seenProperties.Contains(d)))
                {
                    errors.Add(new ValidationError(
                        path.AppendProperty(dependent), "object.dependent-required-missing",
                        $"Property '{dependent}' is required when '{triggerProperty}' is present.", ErrorSeverity.Error));
                }
            }
        }

        if (node.ObjectConstraints is { MinProperties: { } min } && seenProperties.Count < min)
        {
            errors.Add(new ValidationError(
                path, "object.min-properties", $"Object must have at least {min} propert{(min == 1 ? "y" : "ies")}.",
                ErrorSeverity.Error));
        }

        if (node.ObjectConstraints is { MaxProperties: { } max } && seenProperties.Count > max)
        {
            errors.Add(new ValidationError(
                path, "object.max-properties", $"Object must have at most {max} propert{(max == 1 ? "y" : "ies")}.",
                ErrorSeverity.Error));
        }
    }

    private void ValidateArray(
        SchemaNode node, JsonElement value, JsonPath path, IReadOnlyList<LocalDefinition> localDefinitions,
        List<ValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            errors.Add(TypeMismatch(path, "array"));
            return;
        }

        var items = value.EnumerateArray().ToList();

        for (var i = 0; i < items.Count; i++)
        {
            // Tuple-style prefixItems take precedence positionally; once exhausted, remaining
            // items are validated against the homogeneous ItemsNode (Draft 2020-12 semantics).
            if (i < node.PrefixItems.Count)
            {
                ValidateNode(node.PrefixItems[i], items[i], path.AppendIndex(i), localDefinitions, errors);
            }
            else if (node.ItemsNode is not null)
            {
                ValidateNode(node.ItemsNode, items[i], path.AppendIndex(i), localDefinitions, errors);
            }
        }

        if (node.ArrayConstraints is { MinItems: { } min } && items.Count < min)
        {
            errors.Add(new ValidationError(
                path, "array.min-items", $"Array must have at least {min} item{(min == 1 ? "" : "s")}.", ErrorSeverity.Error));
        }

        if (node.ArrayConstraints is { MaxItems: { } max } && items.Count > max)
        {
            errors.Add(new ValidationError(
                path, "array.max-items", $"Array must have at most {max} item{(max == 1 ? "" : "s")}.", ErrorSeverity.Error));
        }

        if (node.ArrayConstraints is { UniqueItems: true })
        {
            var seenTexts = new HashSet<string>();
            foreach (var item in items)
            {
                if (!seenTexts.Add(JsonSerializer.Serialize(item)))
                {
                    errors.Add(new ValidationError(path, "array.duplicate-items", "Array items must be unique.", ErrorSeverity.Error));
                    break;
                }
            }
        }
    }

    private void ValidateString(SchemaNode node, JsonElement value, JsonPath path, List<ValidationError> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(TypeMismatch(path, "string"));
            return;
        }

        var text = value.GetString()!;
        var constraints = node.StringConstraints;

        if (constraints is { MinLength: { } minLength } && text.Length < minLength)
        {
            errors.Add(new ValidationError(path, "string.min-length", $"String must be at least {minLength} characters.", ErrorSeverity.Error));
        }

        if (constraints is { MaxLength: { } maxLength } && text.Length > maxLength)
        {
            errors.Add(new ValidationError(path, "string.max-length", $"String must be at most {maxLength} characters.", ErrorSeverity.Error));
        }

        if (constraints?.Pattern is { } pattern && !Regex.IsMatch(text, pattern))
        {
            errors.Add(new ValidationError(path, "string.pattern-mismatch", $"String does not match pattern '{pattern}'.", ErrorSeverity.Error));
        }

        if (constraints?.Format is { } format && !SchemaFormatValidator.IsValid(format, text))
        {
            errors.Add(new ValidationError(path, "string.format-mismatch", $"String is not a valid {format}.", ErrorSeverity.Error));
        }
    }

    private void ValidateNumeric(SchemaNode node, JsonElement value, JsonPath path, List<ValidationError> errors, bool allowNonInteger)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            errors.Add(TypeMismatch(path, allowNonInteger ? "number" : "integer"));
            return;
        }

        var number = value.GetDecimal();

        if (!allowNonInteger && number != Math.Truncate(number))
        {
            errors.Add(new ValidationError(path, "number.not-an-integer", "Value must be an integer.", ErrorSeverity.Error));
        }

        var constraints = node.NumericConstraints;
        if (constraints is null) return;

        if (constraints.Minimum is { } min)
        {
            var violatesMinimum = constraints.ExclusiveMinimum ? number <= min : number < min;
            if (violatesMinimum)
            {
                errors.Add(new ValidationError(
                    path, "number.below-minimum",
                    $"Value must be {(constraints.ExclusiveMinimum ? "greater than" : "at least")} {min}.", ErrorSeverity.Error));
            }
        }

        if (constraints.Maximum is { } max)
        {
            var violatesMaximum = constraints.ExclusiveMaximum ? number >= max : number > max;
            if (violatesMaximum)
            {
                errors.Add(new ValidationError(
                    path, "number.above-maximum",
                    $"Value must be {(constraints.ExclusiveMaximum ? "less than" : "at most")} {max}.", ErrorSeverity.Error));
            }
        }

        if (constraints.MultipleOf is { } multipleOf && multipleOf != 0 && number % multipleOf != 0)
        {
            errors.Add(new ValidationError(path, "number.not-a-multiple", $"Value must be a multiple of {multipleOf}.", ErrorSeverity.Error));
        }
    }

    private void ValidateComposition(
        SchemaNode node, JsonElement value, JsonPath path, IReadOnlyList<LocalDefinition> localDefinitions,
        List<ValidationError> errors)
    {
        var branchResults = node.CompositionBranches
            .Select(branch => ValidateNodeIsolated(branch, value, path, localDefinitions))
            .ToList();
        var passingCount = branchResults.Count(r => r.Count == 0);

        switch (node.Composition!.Value)
        {
            case CompositionKind.OneOf when passingCount != 1:
                errors.Add(new ValidationError(
                    path, "composition.one-of-mismatch",
                    $"Value must match exactly one of {node.CompositionBranches.Count} schemas, matched {passingCount}.",
                    ErrorSeverity.Error));
                break;
            case CompositionKind.AnyOf when passingCount == 0:
                errors.Add(new ValidationError(
                    path, "composition.any-of-mismatch", "Value does not match any of the allowed schemas.", ErrorSeverity.Error));
                break;
            case CompositionKind.AllOf when passingCount != node.CompositionBranches.Count:
                errors.Add(new ValidationError(
                    path, "composition.all-of-mismatch", "Value does not match all required schemas.", ErrorSeverity.Error));
                break;
            case CompositionKind.Not when passingCount > 0:
                errors.Add(new ValidationError(
                    path, "composition.not-mismatch", "Value must not match the excluded schema.", ErrorSeverity.Error));
                break;
        }
    }

    private void ValidateConditional(
        SchemaNode node, JsonElement value, JsonPath path, IReadOnlyList<LocalDefinition> localDefinitions,
        List<ValidationError> errors)
    {
        var conditionPasses = ValidateNodeIsolated(node.IfNode!, value, path, localDefinitions).Count == 0;
        var branch = conditionPasses ? node.ThenNode : node.ElseNode;

        if (branch is not null)
        {
            ValidateNode(branch, value, path, localDefinitions, errors);
        }
    }

    private static ValidationError TypeMismatch(JsonPath path, string expectedKind) =>
        new(path, "type.mismatch", $"Value must be of type {expectedKind}.", ErrorSeverity.Error);

    private static bool JsonValuesEqual(JsonElement a, JsonElement b) =>
        JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b);
}
