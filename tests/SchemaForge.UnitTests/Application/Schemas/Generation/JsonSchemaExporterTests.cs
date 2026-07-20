using System.Text.Json;
using FluentAssertions;
using SchemaForge.Application.Schemas.Generation;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Application.Schemas.Generation;

public class JsonSchemaExporterTests
{
    private static SchemaVersion NewDraft() => SchemaVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);

    private static JsonElement Export(SchemaVersion version)
    {
        var json = new JsonSchemaExporter().ExportAsync(version, CancellationToken.None).GetAwaiter().GetResult();
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public async Task Exports_the_draft_2020_12_dialect_uri()
    {
        var version = NewDraft();

        var json = await new JsonSchemaExporter().ExportAsync(version, CancellationToken.None);
        var root = JsonDocument.Parse(json).RootElement;

        root.GetProperty("$schema").GetString().Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void Nullable_translates_to_a_type_array_not_a_nullable_keyword()
    {
        var version = NewDraft();
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "middleName", NodeKind.String).Value;
        version.UpdateNode(nodeId, SchemaNodeContent.Empty(NodeKind.String) with { IsNullable = true });

        var root = Export(version);
        var type = root.GetProperty("properties").GetProperty("middleName").GetProperty("type");

        type.ValueKind.Should().Be(JsonValueKind.Array);
        type.EnumerateArray().Select(e => e.GetString()).Should().BeEquivalentTo("string", "null");
    }

    [Fact]
    public void Required_by_parent_aggregates_into_the_parents_required_array()
    {
        var version = NewDraft();
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.Number).Value;
        version.UpdateNode(nodeId, SchemaNodeContent.Empty(NodeKind.Number) with { IsRequiredByParent = true });
        version.AddObjectProperty(version.RootNode.Id, "note", NodeKind.String);

        var root = Export(version);
        var required = root.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();

        required.Should().ContainSingle().Which.Should().Be("amount");
    }

    [Fact]
    public void String_constraints_and_format_are_exported()
    {
        var version = NewDraft();
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "email", NodeKind.String).Value;
        version.UpdateNode(nodeId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new StringConstraints(3, 255, null, SchemaFormat.Email, null),
        });

        var root = Export(version);
        var email = root.GetProperty("properties").GetProperty("email");

        email.GetProperty("minLength").GetInt32().Should().Be(3);
        email.GetProperty("maxLength").GetInt32().Should().Be(255);
        email.GetProperty("format").GetString().Should().Be("email");
    }

    [Fact]
    public void Array_with_items_node_exports_the_items_keyword()
    {
        var version = NewDraft();
        var arrayId = version.AddObjectProperty(version.RootNode.Id, "tags", NodeKind.Array).Value;
        version.SetArrayItemsNode(arrayId, NodeKind.String);

        var root = Export(version);
        var tags = root.GetProperty("properties").GetProperty("tags");

        tags.GetProperty("type").GetString().Should().Be("array");
        tags.GetProperty("items").GetProperty("type").GetString().Should().Be("string");
    }

    [Fact]
    public void Composition_exports_the_matching_keyword_with_every_branch()
    {
        var version = NewDraft();
        var payerId = version.AddObjectProperty(version.RootNode.Id, "payer", null).Value;
        version.UpdateNode(payerId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.OneOf });
        version.AddCompositionBranch(payerId, NodeKind.String);
        version.AddCompositionBranch(payerId, NodeKind.Object);

        var root = Export(version);
        var oneOf = root.GetProperty("properties").GetProperty("payer").GetProperty("oneOf");

        oneOf.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void Local_definition_reference_resolves_to_a_defs_ref()
    {
        var version = NewDraft();
        var localDefId = version.AddLocalDefinition("Category", NodeKind.Object).Value;
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "category", null).Value;
        version.UpdateNode(nodeId, SchemaNodeContent.Empty(null) with { LocalDefinitionRef = localDefId });

        var root = Export(version);
        var categoryRef = root.GetProperty("properties").GetProperty("category").GetProperty("$ref").GetString();

        categoryRef.Should().Be("#/$defs/Category");
        root.GetProperty("$defs").GetProperty("Category").ValueKind.Should().Be(JsonValueKind.Object);
    }
}
