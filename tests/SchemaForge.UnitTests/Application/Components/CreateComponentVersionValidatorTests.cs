using FluentAssertions;
using SchemaForge.Application.Components.Commands.CreateComponentVersion;
using SchemaForge.Application.Schemas;

namespace SchemaForge.UnitTests.Application.Components;

public class CreateComponentVersionValidatorTests
{
    private readonly CreateComponentVersionValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new CreateComponentVersionCommand(Guid.NewGuid(), VersionBumpKind.Minor, "notes"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_component_definition_id_fails()
    {
        var result = _validator.Validate(new CreateComponentVersionCommand(Guid.Empty, VersionBumpKind.Minor, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_change_summary_is_allowed()
    {
        var result = _validator.Validate(new CreateComponentVersionCommand(Guid.NewGuid(), VersionBumpKind.Patch, null));

        result.IsValid.Should().BeTrue();
    }
}
