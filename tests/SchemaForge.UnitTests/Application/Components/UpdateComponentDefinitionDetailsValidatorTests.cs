using FluentAssertions;
using SchemaForge.Application.Components.Commands.UpdateComponentDefinitionDetails;

namespace SchemaForge.UnitTests.Application.Components;

public class UpdateComponentDefinitionDetailsValidatorTests
{
    private readonly UpdateComponentDefinitionDetailsValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new UpdateComponentDefinitionDetailsCommand(Guid.NewGuid(), "MailingAddress", "Updated"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_name_fails()
    {
        var result = _validator.Validate(new UpdateComponentDefinitionDetailsCommand(Guid.NewGuid(), "", null));

        result.IsValid.Should().BeFalse();
    }
}
