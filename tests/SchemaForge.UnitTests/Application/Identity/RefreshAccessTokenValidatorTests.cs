using FluentAssertions;
using SchemaForge.Application.Identity.Commands.RefreshAccessToken;

namespace SchemaForge.UnitTests.Application.Identity;

public class RefreshAccessTokenValidatorTests
{
    private readonly RefreshAccessTokenValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new RefreshAccessTokenCommand("some-refresh-token"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Blank_refresh_token_fails()
    {
        var result = _validator.Validate(new RefreshAccessTokenCommand(""));

        result.IsValid.Should().BeFalse();
    }
}
