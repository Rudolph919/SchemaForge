using System.Text.Json;
using FluentAssertions;
using SchemaForge.Application.Validation.Commands.ValidateJsonPayload;

namespace SchemaForge.UnitTests.Application.Validation;

public class ValidateJsonPayloadValidatorTests
{
    private readonly ValidateJsonPayloadValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(
            new ValidateJsonPayloadCommand(Guid.NewGuid(), JsonDocument.Parse("{}").RootElement));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_schema_version_id_fails()
    {
        var result = _validator.Validate(
            new ValidateJsonPayloadCommand(Guid.Empty, JsonDocument.Parse("{}").RootElement));

        result.IsValid.Should().BeFalse();
    }
}
