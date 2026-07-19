using FluentAssertions;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.SharedKernel.Primitives;

public class EmailAddressTests
{
    [Fact]
    public void Valid_email_is_normalized_to_lowercase()
    {
        var email = EmailAddress.Create("  Someone@Example.COM  ");

        email.Value.Should().Be("someone@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    public void Invalid_emails_are_rejected(string value)
    {
        var act = () => EmailAddress.Create(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_is_structural()
    {
        var first = EmailAddress.Create("someone@example.com");
        var second = EmailAddress.Create("SOMEONE@example.com");

        first.Should().Be(second);
    }
}
