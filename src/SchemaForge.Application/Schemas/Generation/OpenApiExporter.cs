using System.Text.Json;
using System.Text.Json.Nodes;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

// Reuses JsonSchemaNodeWriter directly - OpenAPI 3.1's schema object is JSON-Schema-compatible
// (Step 9 §3). Wraps the same node translation in a minimal OpenAPI document shell rather than a
// full API surface, since a SchemaVersion has no associated HTTP operations of its own to
// describe - "export as OpenAPI" here means "the data shape as a reusable OpenAPI component
// schema," which is what a consumer wiring this into their own API definition actually wants.
public sealed class OpenApiExporter : ISchemaExporter
{
    private static readonly JsonSerializerOptions PrettyPrint = new() { WriteIndented = true };

    public string FormatKey => "openapi";

    public Task<string> ExportAsync(SchemaVersion version, CancellationToken cancellationToken)
    {
        var schema = JsonSchemaNodeWriter.WriteVersion(version);
        schema.Remove("$schema"); // Not a valid keyword inside an OpenAPI schema object.

        var document = new JsonObject
        {
            ["openapi"] = "3.1.0",
            ["info"] = new JsonObject { ["title"] = "SchemaForge Export", ["version"] = version.VersionNumber.ToString() },
            ["paths"] = new JsonObject(),
            ["components"] = new JsonObject { ["schemas"] = new JsonObject { ["Schema"] = schema } },
        };

        return Task.FromResult(document.ToJsonString(PrettyPrint));
    }
}
