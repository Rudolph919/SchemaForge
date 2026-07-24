using FluentAssertions;
using SchemaForge.Domain.Identity;

namespace SchemaForge.UnitTests.Domain.Identity;

public class RefreshTokenTests
{
    [Fact]
    public void Issue_creates_an_active_token()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), Guid.NewGuid(), "hashed-value");

        token.IsActive.Should().BeTrue();
        token.RevokedAt.Should().BeNull();
        token.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Revoke_makes_the_token_inactive()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), Guid.NewGuid(), "hashed-value");

        token.Revoke();

        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void Revoke_records_the_replacement_token_id_when_given_one()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), Guid.NewGuid(), "hashed-value");
        var replacementId = Guid.NewGuid();

        token.Revoke(replacementId);

        token.ReplacedByTokenId.Should().Be(replacementId);
    }
}
