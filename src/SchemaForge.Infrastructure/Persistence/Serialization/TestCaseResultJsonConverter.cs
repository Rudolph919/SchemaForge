using System.Text.Json;
using SchemaForge.Domain.Testing;

namespace SchemaForge.Infrastructure.Persistence.Serialization;

// TestCaseResult/ValidationError are plain immutable records - same reasoning as
// TestExpectationJsonConverter, sharing the same JsonPath converter.
public static class TestCaseResultJsonConverter
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JsonPathJsonConverter() } };

    public static string Serialize(IReadOnlyList<TestCaseResult> results) => JsonSerializer.Serialize(results, Options);

    public static List<TestCaseResult> Deserialize(string json) =>
        JsonSerializer.Deserialize<List<TestCaseResult>>(json, Options)!;
}
