using System.Text.Json;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Generation;

// Named for the dialect it handles, not just "ISchemaImporter" - Step 9 §3 notes a second dialect
// handler (Draft-07, OpenAPI 3.1) could be added later without this one changing, so the name
// leaves room for that rather than implying it's the only importer that will ever exist.
public interface IJsonSchemaImporter
{
    // Populates an already-created Draft SchemaVersion by calling the exact same
    // SchemaVersion.AddObjectProperty/SetArrayItemsNode/... methods a human editing in the
    // Designer would call (the same "every invariant applies identically" principle Step 9 §2
    // established for AI-suggested nodes) - there's no separate bulk-write path that bypasses
    // the aggregate's own rules.
    Result Import(SchemaVersion version, JsonElement schemaDocument);
}
