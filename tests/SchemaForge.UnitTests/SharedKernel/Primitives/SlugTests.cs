using FluentAssertions;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.SharedKernel.Primitives;

public class SlugTests
{
    [Theory]
    [InlineData("acme-corp")]
    [InlineData("acme")]
    [InlineData("acme-corp-2")]
    public void Valid_slugs_are_accepted(string value)
    {
        var slug = Slug.Create(value);

        slug.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Acme-Corp")]
    [InlineData("acme_corp")]
    [InlineData("-acme")]
    [InlineData("acme-")]
    public void Invalid_slugs_are_rejected(string value)
    {
        var act = () => Slug.Create(value);

        act.Should().Throw<ArgumentException>();
    }
}
