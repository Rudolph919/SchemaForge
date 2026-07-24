using FluentAssertions;
using SchemaForge.Application.Identity.Commands.Logout;

namespace SchemaForge.UnitTests.Application.Identity;

public class LogoutValidatorTests
{
    private readonly LogoutValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new LogoutCommand("some-refresh-token"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Blank_refresh_token_fails()
    {
        var result = _validator.Validate(new LogoutCommand(""));

        result.IsValid.Should().BeFalse();
    }
}
