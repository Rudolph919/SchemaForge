using System.Text.Json;
using SchemaForge.Domain.Testing;

namespace SchemaForge.Infrastructure.Persistence.Serialization;

// TestExpectation/ExpectedError are plain immutable records with public constructors, unlike
// SchemaNode - no recursive structure and no invariant-guarded mutation to route through, so
// native System.Text.Json (de)serialization is enough once JsonPath (a value object with a
// validating factory, not a public constructor) has its own converter registered.
public static class TestExpectationJsonConverter
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JsonPathJsonConverter() } };

    public static string Serialize(TestExpectation expectation) => JsonSerializer.Serialize(expectation, Options);

    public static TestExpectation Deserialize(string json) =>
        JsonSerializer.Deserialize<TestExpectation>(json, Options)!;
}
