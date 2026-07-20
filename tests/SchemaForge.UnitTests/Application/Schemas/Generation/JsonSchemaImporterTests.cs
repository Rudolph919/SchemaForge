using System.Text.Json;
using FluentAssertions;
using SchemaForge.Application.Schemas.Generation;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Application.Schemas.Generation;

public class JsonSchemaImporterTests
{
    private static SchemaVersion NewDraft() => SchemaVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);

    private static SchemaVersion ImportRaw(string json)
    {
        var target = NewDraft();
        var result = new JsonSchemaImporter().Import(target, JsonDocument.Parse(json).RootElement);
        if (result.IsFailure) throw new InvalidOperationException(result.Error.Message);
        return target;
    }

    [Fact]
    public void Imports_a_required_string_property_with_constraints()
    {
        var version = ImportRaw("""
            {
              "type": "object",
              "properties": { "invoiceNumber": { "type": "string", "minLength": 3, "maxLength": 20, "pattern": "^INV-" } },
              "required": ["invoiceNumber"]
            }
            """);

        var node = version.RootNode.Properties.Single();
        node.PropertyName.Should().Be("invoiceNumber");
        node.Kind.Should().Be(NodeKind.String);
        node.IsRequiredByParent.Should().BeTrue();
        node.StringConstraints!.MinLength.Should().Be(3);
        node.StringConstraints.MaxLength.Should().Be(20);
        node.StringConstraints.Pattern.Should().Be("^INV-");
    }

    [Fact]
    public void A_type_array_with_null_becomes_IsNullable_not_a_separate_node()
    {
        var version = ImportRaw("""
            {"type": "object", "properties": {"middleName": {"type": ["string", "null"]}}}
            """);

        var node = version.RootNode.Properties.Single();
        node.Kind.Should().Be(NodeKind.String);
        node.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void A_property_not_listed_in_required_is_not_required()
    {
        var version = ImportRaw("""
            {"type": "object", "properties": {"note": {"type": "string"}}, "required": []}
            """);

        version.RootNode.Properties.Single().IsRequiredByParent.Should().BeFalse();
    }

    [Fact]
    public void Nested_objects_and_arrays_import_recursively()
    {
        var version = ImportRaw("""
            {
              "type": "object",
              "properties": {
                "lineItems": {
                  "type": "array",
                  "items": { "type": "object", "properties": { "sku": { "type": "string" } } }
                }
              }
            }
            """);

        var lineItems = version.RootNode.Properties.Single();
        lineItems.Kind.Should().Be(NodeKind.Array);
        lineItems.ItemsNode!.Kind.Should().Be(NodeKind.Object);
        lineItems.ItemsNode.Properties.Single().PropertyName.Should().Be("sku");
    }

    [Fact]
    public void Composition_branches_import_with_the_correct_kind()
    {
        var version = ImportRaw("""
            {
              "type": "object",
              "properties": { "payer": { "oneOf": [{ "type": "string" }, { "type": "object" }] } }
            }
            """);

        var payer = version.RootNode.Properties.Single();
        payer.Composition.Should().Be(CompositionKind.OneOf);
        payer.CompositionBranches.Should().HaveCount(2);
    }

    [Fact]
    public void A_ref_to_a_defs_entry_resolves_to_the_matching_local_definition()
    {
        var version = ImportRaw("""
            {
              "type": "object",
              "$defs": { "Category": { "type": "object", "properties": { "name": { "type": "string" } } } },
              "properties": { "category": { "$ref": "#/$defs/Category" } }
            }
            """);

        version.LocalDefinitions.Should().ContainSingle(d => d.Name == "Category");
        var localDefId = version.LocalDefinitions.Single().Id;
        version.RootNode.Properties.Single().LocalDefinitionRef.Should().Be(localDefId);
    }

    [Fact]
    public async Task Exporting_then_importing_a_version_round_trips_the_tree_structurally()
    {
        var original = NewDraft();
        var amountId = original.AddObjectProperty(original.RootNode.Id, "amount", NodeKind.Number).Value;
        original.UpdateNode(amountId, SchemaNodeContent.Empty(NodeKind.Number) with
        {
            IsRequiredByParent = true,
            NumericConstraints = new NumericConstraints(0, 1000, false, false, null),
        });
        var addressId = original.AddObjectProperty(original.RootNode.Id, "billingAddress", NodeKind.Object).Value;
        original.AddObjectProperty(addressId, "street", NodeKind.String);

        var exported = await new JsonSchemaExporter().ExportAsync(original, CancellationToken.None);

        var reimported = NewDraft();
        var importResult = new JsonSchemaImporter().Import(reimported, JsonDocument.Parse(exported).RootElement);

        importResult.IsSuccess.Should().BeTrue();
        var amount = reimported.RootNode.Properties.Single(p => p.PropertyName == "amount");
        amount.IsRequiredByParent.Should().BeTrue();
        amount.NumericConstraints!.Minimum.Should().Be(0);
        amount.NumericConstraints.Maximum.Should().Be(1000);

        var address = reimported.RootNode.Properties.Single(p => p.PropertyName == "billingAddress");
        address.Properties.Should().ContainSingle(p => p.PropertyName == "street");
    }
}
