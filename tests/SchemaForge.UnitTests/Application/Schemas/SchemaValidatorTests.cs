using System.Text.Json;
using FluentAssertions;
using SchemaForge.Application.Schemas.Validation;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Application.Schemas;

public class SchemaValidatorTests
{
    private readonly SchemaValidator _validator = new();

    private static SchemaVersion NewDraft() => SchemaVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);

    private IReadOnlyList<ValidationError> Validate(SchemaVersion version, string json) =>
        _validator.Validate(version.RootNode, version.LocalDefinitions, JsonDocument.Parse(json).RootElement);

    [Fact]
    public void A_fully_conforming_payload_against_a_rich_tree_produces_no_errors()
    {
        var version = NewDraft();
        var nameId = version.AddObjectProperty(version.RootNode.Id, "name", NodeKind.String).Value;
        version.UpdateNode(nameId, SchemaNodeContent.Empty(NodeKind.String) with { IsRequiredByParent = true });
        var ageId = version.AddObjectProperty(version.RootNode.Id, "age", NodeKind.Integer).Value;
        version.UpdateNode(ageId, SchemaNodeContent.Empty(NodeKind.Integer) with
        {
            NumericConstraints = new NumericConstraints(0, 150, false, false, null),
        });

        var errors = Validate(version, """{"name": "Ada", "age": 36}""");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Wrong_root_type_reports_a_type_mismatch()
    {
        var version = NewDraft();

        var errors = Validate(version, "\"not an object\"");

        errors.Should().ContainSingle(e => e.Code == "type.mismatch" && e.Path.Value == "$");
    }

    [Fact]
    public void Missing_required_property_is_reported_at_its_own_path()
    {
        var version = NewDraft();
        var nameId = version.AddObjectProperty(version.RootNode.Id, "name", NodeKind.String).Value;
        version.UpdateNode(nameId, SchemaNodeContent.Empty(NodeKind.String) with { IsRequiredByParent = true });

        var errors = Validate(version, "{}");

        errors.Should().ContainSingle(e => e.Code == "object.required-property-missing" && e.Path.Value == "$.name");
    }

    [Fact]
    public void Additional_property_is_rejected_when_disallowed()
    {
        var version = NewDraft();
        version.UpdateNode(version.RootNode.Id, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            ObjectConstraints = new ObjectConstraints(null, null, false),
        });

        var errors = Validate(version, """{"unexpected": 1}""");

        errors.Should().ContainSingle(e => e.Code == "object.additional-property-not-allowed");
    }

    [Fact]
    public void Additional_property_is_allowed_by_default()
    {
        var version = NewDraft();

        var errors = Validate(version, """{"anything": 1}""");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void DependentRequired_flags_a_missing_dependent_property()
    {
        var version = NewDraft();
        version.AddObjectProperty(version.RootNode.Id, "creditCardNumber", NodeKind.String);
        version.AddObjectProperty(version.RootNode.Id, "billingAddress", NodeKind.String);
        version.UpdateNode(version.RootNode.Id, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            DependentRequired = new Dictionary<string, IReadOnlyList<string>>
            {
                ["creditCardNumber"] = ["billingAddress"],
            },
        });

        var errors = Validate(version, """{"creditCardNumber": "4111"}""");

        errors.Should().ContainSingle(e => e.Code == "object.dependent-required-missing" && e.Path.Value == "$.billingAddress");
    }

    [Theory]
    [InlineData("\"ab\"", true)]
    [InlineData("\"abc\"", false)]
    [InlineData("\"abcdefghijk\"", true)]
    public void String_length_constraints_are_enforced(string payloadValue, bool expectError)
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "field", NodeKind.String).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new StringConstraints(3, 10, null, null, null),
        });

        var errors = Validate(version, $$"""{"field": {{payloadValue}}}""");

        errors.Any(e => e.Code is "string.min-length" or "string.max-length").Should().Be(expectError);
    }

    [Fact]
    public void String_pattern_mismatch_is_reported()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "sku", NodeKind.String).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new StringConstraints(null, null, "^SKU-[0-9]+$", null, null),
        });

        var errors = Validate(version, """{"sku": "not-a-sku"}""");

        errors.Should().ContainSingle(e => e.Code == "string.pattern-mismatch");
    }

    [Theory]
    [InlineData("\"someone@example.com\"", false)]
    [InlineData("\"not an email\"", true)]
    public void Email_format_is_validated_for_real(string payloadValue, bool expectError)
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "email", NodeKind.String).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new StringConstraints(null, null, null, SchemaFormat.Email, null),
        });

        var errors = Validate(version, $$"""{"email": {{payloadValue}}}""");

        errors.Any(e => e.Code == "string.format-mismatch").Should().Be(expectError);
    }

    [Theory]
    [InlineData("\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", false)]
    [InlineData("\"not-a-uuid\"", true)]
    public void Uuid_format_is_validated(string payloadValue, bool expectError)
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "id", NodeKind.String).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new StringConstraints(null, null, null, SchemaFormat.Uuid, null),
        });

        var errors = Validate(version, $$"""{"id": {{payloadValue}}}""");

        errors.Any(e => e.Code == "string.format-mismatch").Should().Be(expectError);
    }

    [Theory]
    [InlineData(50, false)]
    [InlineData(-1, true)]
    [InlineData(101, true)]
    public void Numeric_range_constraints_are_enforced(int payloadValue, bool expectError)
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "percent", NodeKind.Number).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.Number) with
        {
            NumericConstraints = new NumericConstraints(0, 100, false, false, null),
        });

        var errors = Validate(version, $$"""{"percent": {{payloadValue}}}""");

        errors.Any(e => e.Code is "number.below-minimum" or "number.above-maximum").Should().Be(expectError);
    }

    [Fact]
    public void Exclusive_minimum_rejects_the_boundary_value_itself()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.Number).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.Number) with
        {
            NumericConstraints = new NumericConstraints(0, null, true, false, null),
        });

        var errors = Validate(version, """{"amount": 0}""");

        errors.Should().ContainSingle(e => e.Code == "number.below-minimum");
    }

    [Fact]
    public void MultipleOf_rejects_a_non_multiple()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.Number).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.Number) with
        {
            NumericConstraints = new NumericConstraints(null, null, false, false, 0.01m),
        });

        var errors = Validate(version, """{"amount": 1.005}""");

        errors.Should().ContainSingle(e => e.Code == "number.not-a-multiple");
    }

    [Fact]
    public void Integer_kind_rejects_a_fractional_number()
    {
        var version = NewDraft();
        version.AddObjectProperty(version.RootNode.Id, "count", NodeKind.Integer);

        var errors = Validate(version, """{"count": 1.5}""");

        errors.Should().ContainSingle(e => e.Code == "number.not-an-integer");
    }

    [Fact]
    public void Boolean_type_mismatch_is_reported()
    {
        var version = NewDraft();
        version.AddObjectProperty(version.RootNode.Id, "active", NodeKind.Boolean);

        var errors = Validate(version, """{"active": "yes"}""");

        errors.Should().ContainSingle(e => e.Code == "type.mismatch");
    }

    [Fact]
    public void Nullable_field_accepts_null_but_non_nullable_field_does_not()
    {
        var version = NewDraft();
        var nullableId = version.AddObjectProperty(version.RootNode.Id, "middleName", NodeKind.String).Value;
        version.UpdateNode(nullableId, SchemaNodeContent.Empty(NodeKind.String) with { IsNullable = true });
        var nonNullableId = version.AddObjectProperty(version.RootNode.Id, "lastName", NodeKind.String).Value;

        var nullableErrors = Validate(version, """{"middleName": null}""");
        var nonNullableErrors = Validate(version, """{"lastName": null}""");

        nullableErrors.Should().BeEmpty();
        nonNullableErrors.Should().ContainSingle(e => e.Code == "type.null-not-allowed");
    }

    [Fact]
    public void Const_mismatch_is_reported()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "kind", NodeKind.String).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            ConstValue = JsonLiteral.FromRawJson("\"invoice\""),
        });

        var errors = Validate(version, """{"kind": "receipt"}""");

        errors.Should().ContainSingle(e => e.Code == "const.mismatch");
    }

    [Fact]
    public void Enum_mismatch_is_reported()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "status", NodeKind.String).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            AllowedValues = [JsonLiteral.FromRawJson("\"open\""), JsonLiteral.FromRawJson("\"closed\"")],
        });

        var errors = Validate(version, """{"status": "pending"}""");

        errors.Should().ContainSingle(e => e.Code == "enum.mismatch");
    }

    [Fact]
    public void Array_prefix_items_are_validated_positionally_and_items_node_covers_the_rest()
    {
        var version = NewDraft();
        var arrayId = version.AddObjectProperty(version.RootNode.Id, "coords", NodeKind.Array).Value;
        version.AddArrayPrefixItem(arrayId, NodeKind.Number);
        version.AddArrayPrefixItem(arrayId, NodeKind.Number);
        version.SetArrayItemsNode(arrayId, NodeKind.String);

        var errors = Validate(version, """{"coords": [1, 2, "extra", "more"]}""");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Array_prefix_item_type_mismatch_is_reported_at_its_index()
    {
        var version = NewDraft();
        var arrayId = version.AddObjectProperty(version.RootNode.Id, "coords", NodeKind.Array).Value;
        version.AddArrayPrefixItem(arrayId, NodeKind.Number);

        var errors = Validate(version, """{"coords": ["not a number"]}""");

        errors.Should().ContainSingle(e => e.Code == "type.mismatch" && e.Path.Value == "$.coords[0]");
    }

    [Theory]
    [InlineData("[1,2]", false)]
    [InlineData("[]", true)]
    [InlineData("[1,2,3,4]", true)]
    public void Array_item_count_constraints_are_enforced(string arrayJson, bool expectError)
    {
        var version = NewDraft();
        var arrayId = version.AddObjectProperty(version.RootNode.Id, "items", NodeKind.Array).Value;
        version.UpdateNode(arrayId, SchemaNodeContent.Empty(NodeKind.Array) with
        {
            ArrayConstraints = new ArrayConstraints(1, 3, false),
        });

        var errors = Validate(version, $$"""{"items": {{arrayJson}}}""");

        errors.Any(e => e.Code is "array.min-items" or "array.max-items").Should().Be(expectError);
    }

    [Fact]
    public void UniqueItems_rejects_a_duplicate()
    {
        var version = NewDraft();
        var arrayId = version.AddObjectProperty(version.RootNode.Id, "tags", NodeKind.Array).Value;
        version.UpdateNode(arrayId, SchemaNodeContent.Empty(NodeKind.Array) with
        {
            ArrayConstraints = new ArrayConstraints(null, null, true),
        });

        var errors = Validate(version, """{"tags": ["a", "b", "a"]}""");

        errors.Should().ContainSingle(e => e.Code == "array.duplicate-items");
    }

    [Fact]
    public void OneOf_passes_when_exactly_one_branch_matches()
    {
        var version = NewDraft();
        var payerId = version.AddObjectProperty(version.RootNode.Id, "payer", null).Value;
        version.UpdateNode(payerId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.OneOf });
        var individualId = version.AddCompositionBranch(payerId, NodeKind.Object).Value;
        version.AddObjectProperty(individualId, "fullName", NodeKind.String);
        version.UpdateNode(individualId, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            ObjectConstraints = new ObjectConstraints(null, null, false),
        });
        var orgId = version.AddCompositionBranch(payerId, NodeKind.Object).Value;
        version.AddObjectProperty(orgId, "companyName", NodeKind.String);
        version.UpdateNode(orgId, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            ObjectConstraints = new ObjectConstraints(null, null, false),
        });

        var errors = Validate(version, """{"payer": {"fullName": "Ada Lovelace"}}""");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void OneOf_fails_when_the_value_matches_both_branches()
    {
        var version = NewDraft();
        var payerId = version.AddObjectProperty(version.RootNode.Id, "payer", null).Value;
        version.UpdateNode(payerId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.OneOf });
        version.AddCompositionBranch(payerId, NodeKind.Object); // matches any object, no constraints
        version.AddCompositionBranch(payerId, NodeKind.Object); // matches any object too - ambiguous on purpose

        var errors = Validate(version, """{"payer": {}}""");

        errors.Should().ContainSingle(e => e.Code == "composition.one-of-mismatch");
    }

    [Fact]
    public void AnyOf_passes_when_at_least_one_branch_matches()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "value", null).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.AnyOf });
        version.AddCompositionBranch(fieldId, NodeKind.String);
        version.AddCompositionBranch(fieldId, NodeKind.Number);

        var errors = Validate(version, """{"value": 42}""");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void AllOf_fails_when_any_branch_does_not_match()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "value", NodeKind.String).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(NodeKind.String) with { Composition = CompositionKind.AllOf });
        var branch1 = version.AddCompositionBranch(fieldId, NodeKind.String).Value;
        version.UpdateNode(branch1, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new StringConstraints(5, null, null, null, null),
        });
        var branch2 = version.AddCompositionBranch(fieldId, NodeKind.String).Value;
        version.UpdateNode(branch2, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new StringConstraints(null, 3, null, null, null),
        });

        var errors = Validate(version, """{"value": "abcd"}""");

        errors.Should().Contain(e => e.Code == "composition.all-of-mismatch");
    }

    [Fact]
    public void Not_fails_when_the_excluded_shape_matches()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "value", null).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.Not });
        version.AddCompositionBranch(fieldId, NodeKind.String);

        var errors = Validate(version, """{"value": "should not be a string"}""");

        errors.Should().ContainSingle(e => e.Code == "composition.not-mismatch");
    }

    [Fact]
    public void Not_passes_when_the_excluded_shape_does_not_match()
    {
        var version = NewDraft();
        var fieldId = version.AddObjectProperty(version.RootNode.Id, "value", null).Value;
        version.UpdateNode(fieldId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.Not });
        version.AddCompositionBranch(fieldId, NodeKind.String);

        var errors = Validate(version, """{"value": 42}""");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Conditional_then_branch_applies_when_if_matches()
    {
        var version = NewDraft();
        var shippingId = version.AddObjectProperty(version.RootNode.Id, "shipping", NodeKind.Object).Value;

        var ifId = version.SetConditionalNode(shippingId, ConditionalSlot.If, NodeKind.Object).Value;
        var ifCountryId = version.AddObjectProperty(ifId, "country", NodeKind.String).Value;
        version.UpdateNode(ifCountryId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            ConstValue = JsonLiteral.FromRawJson("\"US\""),
        });

        var thenId = version.SetConditionalNode(shippingId, ConditionalSlot.Then, NodeKind.Object).Value;
        var zipId = version.AddObjectProperty(thenId, "zipCode", NodeKind.String).Value;
        version.UpdateNode(zipId, SchemaNodeContent.Empty(NodeKind.String) with { IsRequiredByParent = true });

        var errorsWithZip = Validate(version, """{"shipping": {"country": "US", "zipCode": "10001"}}""");
        var errorsWithoutZip = Validate(version, """{"shipping": {"country": "US"}}""");

        errorsWithZip.Should().BeEmpty();
        errorsWithoutZip.Should().ContainSingle(e => e.Code == "object.required-property-missing");
    }

    [Fact]
    public void LocalDefinitionRef_validates_recursively_against_the_referenced_definition()
    {
        var version = NewDraft();
        var localDefinitionId = version.AddLocalDefinition("Category", NodeKind.Object).Value;
        var categoryRoot = version.LocalDefinitions.Single(d => d.Id == localDefinitionId).RootNode;
        var nameId = version.AddObjectProperty(categoryRoot.Id, "name", NodeKind.String).Value;
        version.UpdateNode(nameId, SchemaNodeContent.Empty(NodeKind.String) with { IsRequiredByParent = true });
        var subcategoriesId = version.AddObjectProperty(categoryRoot.Id, "subcategories", NodeKind.Array).Value;
        var itemsId = version.SetArrayItemsNode(subcategoriesId, null).Value;
        version.UpdateNode(itemsId, SchemaNodeContent.Empty(null) with { LocalDefinitionRef = localDefinitionId });

        version.UpdateNode(version.RootNode.Id, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            LocalDefinitionRef = localDefinitionId,
        });

        var validPayload = """
            {
                "name": "Electronics",
                "subcategories": [
                    { "name": "Laptops", "subcategories": [] },
                    { "name": "Phones", "subcategories": [ { "name": "Smartphones", "subcategories": [] } ] }
                ]
            }
            """;
        var invalidPayload = """{"subcategories": [{"subcategories": []}]}""";

        Validate(version, validPayload).Should().BeEmpty();
        Validate(version, invalidPayload).Should().Contain(e => e.Code == "object.required-property-missing");
    }
}
