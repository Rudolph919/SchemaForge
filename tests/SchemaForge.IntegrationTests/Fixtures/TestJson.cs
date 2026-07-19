using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchemaForge.IntegrationTests.Fixtures;

// ReadFromJsonAsync's default options are independent of the server's AddJsonOptions
// (JsonStringEnumConverter), so any test deserializing a response with an enum property needs
// this to match what the Api actually sends on the wire.
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
