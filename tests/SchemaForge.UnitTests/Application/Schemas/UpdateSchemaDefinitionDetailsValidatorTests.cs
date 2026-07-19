using FluentAssertions;
using SchemaForge.Application.Schemas.Commands.UpdateSchemaDefinitionDetails;

namespace SchemaForge.UnitTests.Application.Schemas;

public class UpdateSchemaDefinitionDetailsValidatorTests
{
    private readonly UpdateSchemaDefinitionDetailsValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(
            new UpdateSchemaDefinitionDetailsCommand(Guid.NewGuid(), "Invoice Schema", "Vendor invoices", ["finance"]));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_fails(string name)
    {
        var result = _validator.Validate(
            new UpdateSchemaDefinitionDetailsCommand(Guid.NewGuid(), name, null, []));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Blank_tag_fails()
    {
        var result = _validator.Validate(
            new UpdateSchemaDefinitionDetailsCommand(Guid.NewGuid(), "Invoice Schema", null, [""]));

        result.IsValid.Should().BeFalse();
    }
}
