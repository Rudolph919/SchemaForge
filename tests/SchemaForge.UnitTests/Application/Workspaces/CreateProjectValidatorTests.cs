using FluentAssertions;
using SchemaForge.Application.Workspaces.Commands.CreateProject;

namespace SchemaForge.UnitTests.Application.Workspaces;

public class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new CreateProjectCommand("Accounts Payable", "Invoice processing"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_description_is_allowed()
    {
        var result = _validator.Validate(new CreateProjectCommand("Accounts Payable", null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_fails(string name)
    {
        var result = _validator.Validate(new CreateProjectCommand(name, null));

        result.IsValid.Should().BeFalse();
    }
}
