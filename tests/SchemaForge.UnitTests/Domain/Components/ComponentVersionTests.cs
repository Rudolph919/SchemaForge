using FluentAssertions;
using SchemaForge.Domain.Components;
using SchemaForge.Domain.Components.Events;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Domain.Components;

// Mirrors SchemaVersionTests exactly - ComponentVersion shares the entire node-tree machinery
// with SchemaVersion (Step 4 §5, Step 7 §3), so the same coverage that proved SchemaVersion's
// behavior applies verbatim here, just against ComponentVersion's own draft-guard and events.
public class ComponentVersionTests
{
    private static ComponentVersion NewDraft() =>
        ComponentVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);

    [Fact]
    public void CreateDraft_defaults_to_draft_status_with_an_empty_object_root_and_raises_an_event()
    {
        var version = NewDraft();

        version.Status.Should().Be(SchemaLifecycleStatus.Draft);
        version.RootNode.Kind.Should().Be(NodeKind.Object);
        version.RootNode.Properties.Should().BeEmpty();
        version.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ComponentVersionCreated>();
    }

    [Fact]
    public void Publish_transitions_draft_to_published_and_raises_an_event()
    {
        var version = NewDraft();

        var result = version.Publish();

        result.IsSuccess.Should().BeTrue();
        version.Status.Should().Be(SchemaLifecycleStatus.Published);
        version.PublishedAt.Should().NotBeNull();
        version.DomainEvents.Should().Contain(e => e is ComponentVersionPublished);
    }

    [Fact]
    public void Publish_fails_for_an_already_published_version()
    {
        var version = NewDraft();
        version.Publish();

        var result = version.Publish();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Deprecate_transitions_published_to_deprecated_and_raises_an_event()
    {
        var version = NewDraft();
        version.Publish();

        var result = version.Deprecate();

        result.IsSuccess.Should().BeTrue();
        version.Status.Should().Be(SchemaLifecycleStatus.Deprecated);
        version.DomainEvents.Should().Contain(e => e is ComponentVersionDeprecated);
    }

    [Fact]
    public void Deprecate_fails_for_a_draft_version()
    {
        var version = NewDraft();

        var result = version.Deprecate();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddObjectProperty_adds_a_child_to_the_root_and_raises_an_event()
    {
        var version = NewDraft();

        var result = version.AddObjectProperty(version.RootNode.Id, "street", NodeKind.String);

        result.IsSuccess.Should().BeTrue();
        version.RootNode.Properties.Should().ContainSingle(n => n.Id == result.Value && n.PropertyName == "street");
        version.DomainEvents.Should().Contain(e => e is ComponentNodeAdded);
    }

    [Fact]
    public void AddObjectProperty_fails_when_the_parent_node_does_not_exist()
    {
        var version = NewDraft();

        var result = version.AddObjectProperty(Guid.NewGuid(), "street", NodeKind.String);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddObjectProperty_fails_when_the_parent_is_not_an_object()
    {
        var version = NewDraft();
        var stringNodeId = version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.String).Value;

        var result = version.AddObjectProperty(stringNodeId, "nested", NodeKind.String);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddObjectProperty_fails_on_a_duplicate_property_name()
    {
        var version = NewDraft();
        version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.Number);

        var result = version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.String);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddObjectProperty_fails_with_a_blank_property_name(string propertyName)
    {
        var version = NewDraft();

        var result = version.AddObjectProperty(version.RootNode.Id, propertyName, NodeKind.String);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddArrayPrefixItem_adds_a_tuple_style_item_to_an_array_node()
    {
        var version = NewDraft();
        var arrayNodeId = version.AddObjectProperty(version.RootNode.Id, "lines", NodeKind.Array).Value;

        var result = version.AddArrayPrefixItem(arrayNodeId, NodeKind.Object);

        result.IsSuccess.Should().BeTrue();
        var arrayNode = version.RootNode.Properties.Single(n => n.Id == arrayNodeId);
        arrayNode.PrefixItems.Should().ContainSingle(n => n.Id == result.Value);
    }

    [Fact]
    public void AddArrayPrefixItem_fails_when_the_parent_is_not_an_array()
    {
        var version = NewDraft();

        var result = version.AddArrayPrefixItem(version.RootNode.Id, NodeKind.Object);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SetArrayItemsNode_sets_a_homogeneous_item_schema_on_an_array_node()
    {
        var version = NewDraft();
        var arrayNodeId = version.AddObjectProperty(version.RootNode.Id, "tags", NodeKind.Array).Value;

        var result = version.SetArrayItemsNode(arrayNodeId, NodeKind.String);

        result.IsSuccess.Should().BeTrue();
        var arrayNode = version.RootNode.Properties.Single(n => n.Id == arrayNodeId);
        arrayNode.ItemsNode.Should().NotBeNull();
        arrayNode.ItemsNode!.Id.Should().Be(result.Value);
    }

    [Fact]
    public void SetArrayItemsNode_fails_when_the_parent_is_not_an_array()
    {
        var version = NewDraft();

        var result = version.SetArrayItemsNode(version.RootNode.Id, NodeKind.String);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddCompositionBranch_fails_until_a_composition_kind_is_set_on_the_node()
    {
        var version = NewDraft();
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "payer", null).Value;

        var beforeComposition = version.AddCompositionBranch(nodeId, NodeKind.Object);
        beforeComposition.IsFailure.Should().BeTrue();

        version.UpdateNode(nodeId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.OneOf });
        var afterComposition = version.AddCompositionBranch(nodeId, NodeKind.Object);

        afterComposition.IsSuccess.Should().BeTrue();
        var node = version.RootNode.Properties.Single(n => n.Id == nodeId);
        node.CompositionBranches.Should().ContainSingle(n => n.Id == afterComposition.Value);
    }

    [Theory]
    [InlineData(ConditionalSlot.If)]
    [InlineData(ConditionalSlot.Then)]
    [InlineData(ConditionalSlot.Else)]
    public void SetConditionalNode_sets_the_requested_slot(ConditionalSlot slot)
    {
        var version = NewDraft();

        var result = version.SetConditionalNode(version.RootNode.Id, slot, NodeKind.Object);

        result.IsSuccess.Should().BeTrue();
        var actual = slot switch
        {
            ConditionalSlot.If => version.RootNode.IfNode,
            ConditionalSlot.Then => version.RootNode.ThenNode,
            ConditionalSlot.Else => version.RootNode.ElseNode,
            _ => throw new ArgumentOutOfRangeException(nameof(slot)),
        };
        actual.Should().NotBeNull();
        actual!.Id.Should().Be(result.Value);
    }

    [Fact]
    public void UpdateNode_applies_new_content_and_raises_an_event()
    {
        var version = NewDraft();
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "postalCode", NodeKind.String).Value;

        var content = SchemaNodeContent.Empty(NodeKind.String) with
        {
            Description = "The postal code",
            IsRequiredByParent = true,
            StringConstraints = new(null, 10, null, null, null),
        };
        var result = version.UpdateNode(nodeId, content);

        result.IsSuccess.Should().BeTrue();
        var node = version.RootNode.Properties.Single(n => n.Id == nodeId);
        node.Description.Should().Be("The postal code");
        node.IsRequiredByParent.Should().BeTrue();
        node.StringConstraints!.MaxLength.Should().Be(10);
        version.DomainEvents.Should().Contain(e => e is ComponentNodeUpdated);
    }

    [Fact]
    public void UpdateNode_fails_when_the_node_does_not_exist()
    {
        var version = NewDraft();

        var result = version.UpdateNode(Guid.NewGuid(), SchemaNodeContent.Empty(NodeKind.String));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MoveNode_updates_the_nodes_order()
    {
        var version = NewDraft();
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.Number).Value;

        var result = version.MoveNode(nodeId, 5);

        result.IsSuccess.Should().BeTrue();
        version.RootNode.Properties.Single(n => n.Id == nodeId).Order.Should().Be(5);
    }

    [Fact]
    public void MoveNode_cannot_move_the_root_node()
    {
        var version = NewDraft();

        var result = version.MoveNode(version.RootNode.Id, 1);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MoveNode_fails_when_the_node_does_not_exist()
    {
        var version = NewDraft();

        var result = version.MoveNode(Guid.NewGuid(), 1);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveNode_removes_a_nested_property_and_raises_an_event()
    {
        var version = NewDraft();
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.Number).Value;

        var result = version.RemoveNode(nodeId);

        result.IsSuccess.Should().BeTrue();
        version.RootNode.Properties.Should().BeEmpty();
        version.DomainEvents.Should().Contain(e => e is ComponentNodeRemoved);
    }

    [Fact]
    public void RemoveNode_removes_a_node_nested_several_levels_deep()
    {
        var version = NewDraft();
        var arrayId = version.AddObjectProperty(version.RootNode.Id, "items", NodeKind.Array).Value;
        var itemsNodeId = version.SetArrayItemsNode(arrayId, NodeKind.Object).Value;
        var nestedFieldId = version.AddObjectProperty(itemsNodeId, "sku", NodeKind.String).Value;

        var result = version.RemoveNode(nestedFieldId);

        result.IsSuccess.Should().BeTrue();
        var itemsNode = version.RootNode.Properties.Single(n => n.Id == arrayId).ItemsNode!;
        itemsNode.Properties.Should().BeEmpty();
    }

    [Fact]
    public void RemoveNode_cannot_remove_the_root_node()
    {
        var version = NewDraft();

        var result = version.RemoveNode(version.RootNode.Id);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveNode_fails_when_the_node_does_not_exist()
    {
        var version = NewDraft();

        var result = version.RemoveNode(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddLocalDefinition_creates_a_reusable_local_definition_and_nodes_within_it_are_reachable()
    {
        var version = NewDraft();

        var defResult = version.AddLocalDefinition("Category", NodeKind.Object);
        defResult.IsSuccess.Should().BeTrue();

        var localDefinition = version.LocalDefinitions.Single(d => d.Id == defResult.Value);
        var propertyResult = version.AddObjectProperty(localDefinition.RootNode.Id, "name", NodeKind.String);

        propertyResult.IsSuccess.Should().BeTrue();
        localDefinition.RootNode.Properties.Should().ContainSingle(n => n.Id == propertyResult.Value);
    }

    [Fact]
    public void AddLocalDefinition_fails_on_a_duplicate_name()
    {
        var version = NewDraft();
        version.AddLocalDefinition("Category", NodeKind.Object);

        var result = version.AddLocalDefinition("Category", NodeKind.Object);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveLocalDefinition_removes_it()
    {
        var version = NewDraft();
        var id = version.AddLocalDefinition("Category", NodeKind.Object).Value;

        var result = version.RemoveLocalDefinition(id);

        result.IsSuccess.Should().BeTrue();
        version.LocalDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveLocalDefinition_fails_when_it_does_not_exist()
    {
        var version = NewDraft();

        var result = version.RemoveLocalDefinition(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Every_mutating_method_fails_once_the_version_is_published()
    {
        var version = NewDraft();
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.Number).Value;
        version.Publish();

        version.AddObjectProperty(version.RootNode.Id, "another", NodeKind.String).IsFailure.Should().BeTrue();
        version.UpdateNode(nodeId, SchemaNodeContent.Empty(NodeKind.Number)).IsFailure.Should().BeTrue();
        version.MoveNode(nodeId, 5).IsFailure.Should().BeTrue();
        version.RemoveNode(nodeId).IsFailure.Should().BeTrue();
        version.AddLocalDefinition("Category", NodeKind.Object).IsFailure.Should().BeTrue();
    }
}
