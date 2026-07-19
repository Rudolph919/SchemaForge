using FluentAssertions;
using SchemaForge.Application.Schemas;
using SchemaForge.Application.Schemas.Commands.AddSchemaNode;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.UnitTests.Application.Schemas;

public class AddSchemaNodeValidatorTests
{
    private readonly AddSchemaNodeValidator _validator = new();

    [Fact]
    public void Object_property_with_a_property_name_passes()
    {
        var result = _validator.Validate(new AddSchemaNodeCommand(
            Guid.NewGuid(), Guid.NewGuid(), NodeAttachmentKind.ObjectProperty, "amount", NodeKind.Number));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Object_property_without_a_property_name_fails()
    {
        var result = _validator.Validate(new AddSchemaNodeCommand(
            Guid.NewGuid(), Guid.NewGuid(), NodeAttachmentKind.ObjectProperty, null, NodeKind.Number));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Array_items_with_a_property_name_fails()
    {
        var result = _validator.Validate(new AddSchemaNodeCommand(
            Guid.NewGuid(), Guid.NewGuid(), NodeAttachmentKind.ArrayItems, "shouldNotBeHere", NodeKind.String));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Array_items_without_a_property_name_passes()
    {
        var result = _validator.Validate(new AddSchemaNodeCommand(
            Guid.NewGuid(), Guid.NewGuid(), NodeAttachmentKind.ArrayItems, null, NodeKind.String));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ids_fail()
    {
        var result = _validator.Validate(new AddSchemaNodeCommand(
            Guid.Empty, Guid.Empty, NodeAttachmentKind.CompositionBranch, null, null));

        result.IsValid.Should().BeFalse();
    }
}
