namespace SchemaForge.Domain.Schemas;

// A computed value, never persisted (Step 2 §5) - comparing two node trees on demand, not an
// entity or aggregate. Paths are a diff-specific structural notation, not JsonPath (which
// describes JSON document navigation - composition branches and conditional slots aren't JSON
// document positions, so they need their own notation: "$.payer.oneOf[0]", "$.root.if").
public sealed record SchemaDiff(
    IReadOnlyList<string> AddedPaths,
    IReadOnlyList<string> RemovedPaths,
    IReadOnlyList<SchemaDiffChange> ChangedPaths);

public sealed record SchemaDiffChange(string Path, IReadOnlyList<string> Changes);
