using FluentAssertions;
using SchemaForge.Application.Components.Commands.CreateComponentDefinition;

namespace SchemaForge.UnitTests.Application.Components;

public class CreateComponentDefinitionValidatorTests
{
    private readonly CreateComponentDefinitionValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new CreateComponentDefinitionCommand("PostalAddress", "Reusable address"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.Validate(new CreateComponentDefinitionCommand("", null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_description_is_allowed()
    {
        var result = _validator.Validate(new CreateComponentDefinitionCommand("PostalAddress", null));

        result.IsValid.Should().BeTrue();
    }
}
