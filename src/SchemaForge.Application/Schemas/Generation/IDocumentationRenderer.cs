using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas.Generation;

// Same registry shape as ISchemaExporter (Step 9 §3) - registered as IEnumerable<IDocumentationRenderer>,
// dispatched by matching FormatKey against ?format=. Named "Renderer" not "Generator" to match the
// architecture doc's own naming for this interface, even though the roadmap's prose calls it
// "IDocumentationGenerator" informally.
public interface IDocumentationRenderer
{
    string FormatKey { get; }

    Task<string> RenderAsync(SchemaVersion version, CancellationToken cancellationToken);
}
