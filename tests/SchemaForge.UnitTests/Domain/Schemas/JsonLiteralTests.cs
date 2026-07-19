using FluentAssertions;
using SchemaForge.Domain.Schemas.ValueObjects;

namespace SchemaForge.UnitTests.Domain.Schemas;

public class JsonLiteralTests
{
    [Fact]
    public void Differently_formatted_equivalent_json_canonicalizes_to_the_same_value()
    {
        var a = JsonLiteral.FromRawJson("{\"a\":1,\"b\":2}");
        var b = JsonLiteral.FromRawJson("{ \"a\": 1, \"b\": 2 }");

        a.Should().Be(b);
    }

    [Fact]
    public void Structurally_different_json_is_not_equal()
    {
        var a = JsonLiteral.FromRawJson("{\"a\":1}");
        var b = JsonLiteral.FromRawJson("{\"a\":2}");

        a.Should().NotBe(b);
    }

    [Fact]
    public void FromRawJson_rejects_invalid_json()
    {
        var act = () => JsonLiteral.FromRawJson("not json");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void FromValue_serializes_a_CLR_value()
    {
        var literal = JsonLiteral.FromValue(42);

        literal.RawJson.Should().Be("42");
    }

    [Fact]
    public void AsJsonElement_round_trips_back_to_a_readable_element()
    {
        var literal = JsonLiteral.FromRawJson("\"hello\"");

        var element = literal.AsJsonElement();

        element.GetString().Should().Be("hello");
    }
}
