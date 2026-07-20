using FluentAssertions;
using SchemaForge.Application.Components.Commands.MoveComponentNode;

namespace SchemaForge.UnitTests.Application.Components;

public class MoveComponentNodeValidatorTests
{
    private readonly MoveComponentNodeValidator _validator = new();

    [Fact]
    public void Non_negative_order_passes()
    {
        var result = _validator.Validate(new MoveComponentNodeCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Negative_order_fails()
    {
        var result = _validator.Validate(new MoveComponentNodeCommand(Guid.NewGuid(), Guid.NewGuid(), -1));

        result.IsValid.Should().BeFalse();
    }
}
