namespace SchemaForge.Application.Schemas.Generation;

// Shared by every IDocumentationRenderer - the tree -> flattened field list walk is one piece of
// logic (mirrors JsonSchemaNodeWriter's role for the exporters), each renderer just formats the
// same model differently. Path notation matches SchemaDiffComputer's, for the same reason
// (composition branches and conditional slots aren't JSON document positions).
public sealed record DocumentationField(
    string Path,
    string? PropertyName,
    int Depth,
    string Kind,
    bool Required,
    bool Nullable,
    string? Description,
    IReadOnlyList<string> Constraints);
