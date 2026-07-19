using FluentAssertions;
using SchemaForge.Application.Organizations.Commands.CreateTeam;

namespace SchemaForge.UnitTests.Application.Organizations;

public class CreateTeamValidatorTests
{
    private readonly CreateTeamValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new CreateTeamCommand("Platform", "Handles platform work"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_description_is_allowed()
    {
        var result = _validator.Validate(new CreateTeamCommand("Platform", null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_fails(string name)
    {
        var result = _validator.Validate(new CreateTeamCommand(name, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Name_over_max_length_fails()
    {
        var result = _validator.Validate(new CreateTeamCommand(new string('a', 201), null));

        result.IsValid.Should().BeFalse();
    }
}
