using FluentAssertions;
using SchemaForge.Application.Schemas;
using SchemaForge.Application.Schemas.Commands.CreateSchemaVersion;

namespace SchemaForge.UnitTests.Application.Schemas;

public class CreateSchemaVersionValidatorTests
{
    private readonly CreateSchemaVersionValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new CreateSchemaVersionCommand(Guid.NewGuid(), VersionBumpKind.Minor, "notes"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_schema_definition_id_fails()
    {
        var result = _validator.Validate(new CreateSchemaVersionCommand(Guid.Empty, VersionBumpKind.Minor, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_change_summary_is_allowed()
    {
        var result = _validator.Validate(new CreateSchemaVersionCommand(Guid.NewGuid(), VersionBumpKind.Patch, null));

        result.IsValid.Should().BeTrue();
    }
}
