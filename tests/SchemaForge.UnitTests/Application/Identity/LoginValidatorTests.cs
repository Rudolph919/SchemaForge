using FluentAssertions;
using SchemaForge.Application.Identity.Commands.Login;

namespace SchemaForge.UnitTests.Application.Identity;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new LoginCommand("ada@example.com", "correct-horse-battery"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "correct-horse-battery")]
    [InlineData("not-an-email", "correct-horse-battery")]
    [InlineData("ada@example.com", "")]
    public void Invalid_command_fails(string email, string password)
    {
        var result = _validator.Validate(new LoginCommand(email, password));

        result.IsValid.Should().BeFalse();
    }
}
