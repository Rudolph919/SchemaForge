using FluentAssertions;
using SchemaForge.Application.Identity.Commands.RegisterUser;

namespace SchemaForge.UnitTests.Application.Identity;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var command = new RegisterUserCommand("ada@example.com", "correct-horse-battery", "Ada", "Acme Corp");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "correct-horse-battery", "Ada", "Acme Corp")]
    [InlineData("not-an-email", "correct-horse-battery", "Ada", "Acme Corp")]
    [InlineData("ada@example.com", "short", "Ada", "Acme Corp")]
    [InlineData("ada@example.com", "correct-horse-battery", "", "Acme Corp")]
    [InlineData("ada@example.com", "correct-horse-battery", "Ada", "")]
    public void Invalid_command_fails(string email, string password, string displayName, string organizationName)
    {
        var command = new RegisterUserCommand(email, password, displayName, organizationName);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
