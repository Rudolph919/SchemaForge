using System.Text.Json;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

public sealed class JsonDocumentationRenderer : IDocumentationRenderer
{
    // Matches the rest of this Api's JSON convention (ASP.NET Core's default camelCase output for
    // Contracts DTOs) - without an explicit naming policy, System.Text.Json serializes
    // DocumentationField's PascalCase record properties as-is instead.
    private static readonly JsonSerializerOptions PrettyPrint = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string FormatKey => "json";

    public Task<string> RenderAsync(SchemaVersion version, CancellationToken cancellationToken)
    {
        var document = new
        {
            versionNumber = version.VersionNumber.ToString(),
            status = version.Status.ToString(),
            changeSummary = version.ChangeSummary,
            fields = DocumentationModelBuilder.Build(version),
        };

        return Task.FromResult(JsonSerializer.Serialize(document, PrettyPrint));
    }
}
