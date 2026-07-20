using FluentAssertions;
using SchemaForge.Application.Components.Commands.AddComponentNode;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.UnitTests.Application.Components;

public class AddComponentNodeValidatorTests
{
    private readonly AddComponentNodeValidator _validator = new();

    [Fact]
    public void Object_property_with_a_property_name_passes()
    {
        var result = _validator.Validate(new AddComponentNodeCommand(
            Guid.NewGuid(), Guid.NewGuid(), NodeAttachmentKind.ObjectProperty, "street", NodeKind.String));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Object_property_without_a_property_name_fails()
    {
        var result = _validator.Validate(new AddComponentNodeCommand(
            Guid.NewGuid(), Guid.NewGuid(), NodeAttachmentKind.ObjectProperty, null, NodeKind.String));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Array_items_with_a_property_name_fails()
    {
        var result = _validator.Validate(new AddComponentNodeCommand(
            Guid.NewGuid(), Guid.NewGuid(), NodeAttachmentKind.ArrayItems, "shouldNotBeHere", NodeKind.String));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_ids_fail()
    {
        var result = _validator.Validate(new AddComponentNodeCommand(
            Guid.Empty, Guid.Empty, NodeAttachmentKind.CompositionBranch, null, null));

        result.IsValid.Should().BeFalse();
    }
}
