using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Contracts.V1.Components;

// Reuses VersionBumpKind directly from Contracts.V1.Schemas - a generic version-bump concept,
// not schema-specific (Step 7 §3), just historically declared there first.
public sealed record CreateComponentVersionRequest(VersionBumpKind BumpKind, string? ChangeSummary);
