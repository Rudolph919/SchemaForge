using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Contracts.V1.Components;

// Reuses SchemaNodeResponse/LocalDefinitionResponse directly from Contracts.V1.Schemas - the
// node-tree shape is identical between a schema version and a component version (Step 4 §5: "no
// new concepts"), so a parallel ComponentNodeResponse would be pure duplication.
public sealed record ComponentVersionDetailResponse(
    Guid Id,
    Guid ComponentDefinitionId,
    string VersionNumber,
    SchemaLifecycleStatus Status,
    string? ChangeSummary,
    DateTimeOffset? PublishedAt,
    SchemaNodeResponse RootNode,
    IReadOnlyList<LocalDefinitionResponse> LocalDefinitions);
