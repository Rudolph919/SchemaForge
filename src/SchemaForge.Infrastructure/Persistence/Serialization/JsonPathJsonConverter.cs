using System.Text.Json;
using System.Text.Json.Serialization;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Infrastructure.Persistence.Serialization;

// JsonPath has a validating factory (Create), not a public constructor, so System.Text.Json
// can't materialize it natively - shared by every converter in this namespace that serializes a
// type containing a JsonPath (TestExpectation, TestCaseResult's ValidationError entries).
internal sealed class JsonPathJsonConverter : JsonConverter<JsonPath>
{
    public override JsonPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        JsonPath.Create(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, JsonPath value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
