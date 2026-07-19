using FluentAssertions;
using SchemaForge.Application.Schemas.Commands.CreateSchemaDefinition;

namespace SchemaForge.UnitTests.Application.Schemas;

public class CreateSchemaDefinitionValidatorTests
{
    private readonly CreateSchemaDefinitionValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new CreateSchemaDefinitionCommand(Guid.NewGuid(), "Invoice Schema", "Vendor invoices"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_description_is_allowed()
    {
        var result = _validator.Validate(new CreateSchemaDefinitionCommand(Guid.NewGuid(), "Invoice Schema", null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_fails(string name)
    {
        var result = _validator.Validate(new CreateSchemaDefinitionCommand(Guid.NewGuid(), name, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_project_id_fails()
    {
        var result = _validator.Validate(new CreateSchemaDefinitionCommand(Guid.Empty, "Invoice Schema", null));

        result.IsValid.Should().BeFalse();
    }
}
