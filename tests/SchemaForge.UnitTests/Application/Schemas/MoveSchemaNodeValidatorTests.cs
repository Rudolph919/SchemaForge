using FluentAssertions;
using SchemaForge.Application.Schemas.Commands.MoveSchemaNode;

namespace SchemaForge.UnitTests.Application.Schemas;

public class MoveSchemaNodeValidatorTests
{
    private readonly MoveSchemaNodeValidator _validator = new();

    [Fact]
    public void Non_negative_order_passes()
    {
        var result = _validator.Validate(new MoveSchemaNodeCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Negative_order_fails()
    {
        var result = _validator.Validate(new MoveSchemaNodeCommand(Guid.NewGuid(), Guid.NewGuid(), -1));

        result.IsValid.Should().BeFalse();
    }
}
