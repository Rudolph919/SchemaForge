using FluentAssertions;
using SchemaForge.Domain.Identity;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Domain.Identity;

public class UserTests
{
    [Fact]
    public void Register_creates_an_unverified_user()
    {
        var email = EmailAddress.Create("ada@example.com");

        var user = User.Register(email, "hashed-password", "Ada Lovelace");

        user.Email.Should().Be(email);
        user.DisplayName.Should().Be("Ada Lovelace");
        user.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public void Register_raises_a_UserRegistered_domain_event()
    {
        var email = EmailAddress.Create("ada@example.com");

        var user = User.Register(email, "hashed-password", "Ada Lovelace");

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegistered>()
            .Which.Email.Should().Be(email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_rejects_a_blank_display_name(string displayName)
    {
        var email = EmailAddress.Create("ada@example.com");

        var act = () => User.Register(email, "hashed-password", displayName);

        act.Should().Throw<ArgumentException>();
    }
}
