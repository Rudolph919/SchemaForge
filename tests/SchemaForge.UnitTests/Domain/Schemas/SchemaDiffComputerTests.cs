using FluentAssertions;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Domain.Schemas;

public class SchemaDiffComputerTests
{
    private static SchemaVersion NewDraft() => SchemaVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);

    [Fact]
    public void Identical_trees_produce_an_empty_diff()
    {
        var a = NewDraft();
        a.AddObjectProperty(a.RootNode.Id, "amount", NodeKind.Number);
        var b = NewDraft();
        b.AddObjectProperty(b.RootNode.Id, "amount", NodeKind.Number);

        var diff = SchemaDiffComputer.Compute(a.RootNode, a.LocalDefinitions, b.RootNode, b.LocalDefinitions);

        diff.AddedPaths.Should().BeEmpty();
        diff.RemovedPaths.Should().BeEmpty();
        diff.ChangedPaths.Should().BeEmpty();
    }

    [Fact]
    public void A_new_top_level_property_is_reported_once_not_once_per_descendant()
    {
        var a = NewDraft();
        var b = NewDraft();
        var payerId = b.AddObjectProperty(b.RootNode.Id, "payer", NodeKind.Object).Value;
        b.AddObjectProperty(payerId, "name", NodeKind.String);
        b.AddObjectProperty(payerId, "taxId", NodeKind.String);

        var diff = SchemaDiffComputer.Compute(a.RootNode, a.LocalDefinitions, b.RootNode, b.LocalDefinitions);

        diff.AddedPaths.Should().ContainSingle().Which.Should().Be("$.payer");
    }

    [Fact]
    public void A_removed_property_is_reported()
    {
        var a = NewDraft();
        a.AddObjectProperty(a.RootNode.Id, "legacyField", NodeKind.String);
        var b = NewDraft();

        var diff = SchemaDiffComputer.Compute(a.RootNode, a.LocalDefinitions, b.RootNode, b.LocalDefinitions);

        diff.RemovedPaths.Should().ContainSingle().Which.Should().Be("$.legacyField");
    }

    [Fact]
    public void A_kind_change_on_the_same_property_is_reported_as_a_change_not_an_add_and_remove()
    {
        var a = NewDraft();
        a.AddObjectProperty(a.RootNode.Id, "amount", NodeKind.String);
        var b = NewDraft();
        b.AddObjectProperty(b.RootNode.Id, "amount", NodeKind.Number);

        var diff = SchemaDiffComputer.Compute(a.RootNode, a.LocalDefinitions, b.RootNode, b.LocalDefinitions);

        diff.AddedPaths.Should().BeEmpty();
        diff.RemovedPaths.Should().BeEmpty();
        diff.ChangedPaths.Should().ContainSingle(c => c.Path == "$.amount");
        diff.ChangedPaths.Single().Changes.Should().ContainSingle(c => c.Contains("kind changed"));
    }

    [Fact]
    public void A_constraint_change_is_reported()
    {
        var a = NewDraft();
        var nodeId = a.AddObjectProperty(a.RootNode.Id, "code", NodeKind.String).Value;
        a.UpdateNode(nodeId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new SchemaForge.Domain.Schemas.ValueObjects.StringConstraints(1, 10, null, null, null),
        });

        var b = NewDraft();
        var nodeId2 = b.AddObjectProperty(b.RootNode.Id, "code", NodeKind.String).Value;
        b.UpdateNode(nodeId2, SchemaNodeContent.Empty(NodeKind.String) with
        {
            StringConstraints = new SchemaForge.Domain.Schemas.ValueObjects.StringConstraints(1, 20, null, null, null),
        });

        var diff = SchemaDiffComputer.Compute(a.RootNode, a.LocalDefinitions, b.RootNode, b.LocalDefinitions);

        diff.ChangedPaths.Should().ContainSingle(c => c.Path == "$.code");
        diff.ChangedPaths.Single().Changes.Should().Contain("string constraints changed");
    }

    [Fact]
    public void Array_items_and_composition_branches_use_distinct_path_notations()
    {
        var a = NewDraft();
        var b = NewDraft();

        var arrayId = b.AddObjectProperty(b.RootNode.Id, "tags", NodeKind.Array).Value;
        b.SetArrayItemsNode(arrayId, NodeKind.String);

        var payerId = b.AddObjectProperty(b.RootNode.Id, "payer", null).Value;
        b.UpdateNode(payerId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.OneOf });
        b.AddCompositionBranch(payerId, NodeKind.Object);

        var diff = SchemaDiffComputer.Compute(a.RootNode, a.LocalDefinitions, b.RootNode, b.LocalDefinitions);

        diff.AddedPaths.Should().Contain("$.tags");
        diff.AddedPaths.Should().Contain("$.payer");
        // tags[] and payer.oneOf[0] are descendants of already-added parents, so they're
        // collapsed away - this asserts that collapsing, not that the paths never existed.
        diff.AddedPaths.Should().NotContain("$.tags[]");
        diff.AddedPaths.Should().NotContain("$.payer.oneOf[0]");
    }

    [Fact]
    public void Local_definitions_are_included_under_a_defs_prefix()
    {
        var a = NewDraft();
        var b = NewDraft();
        b.AddLocalDefinition("Category", NodeKind.Object);

        var diff = SchemaDiffComputer.Compute(a.RootNode, a.LocalDefinitions, b.RootNode, b.LocalDefinitions);

        diff.AddedPaths.Should().ContainSingle().Which.Should().Be("$defs.Category");
    }
}
