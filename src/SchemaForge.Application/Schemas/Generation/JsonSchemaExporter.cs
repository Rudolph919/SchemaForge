using System.Text.Json;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

public sealed class JsonSchemaExporter : ISchemaExporter
{
    private static readonly JsonSerializerOptions PrettyPrint = new() { WriteIndented = true };

    public string FormatKey => "json-schema";

    public Task<string> ExportAsync(SchemaVersion version, CancellationToken cancellationToken) =>
        Task.FromResult(JsonSchemaNodeWriter.WriteVersion(version).ToJsonString(PrettyPrint));
}
