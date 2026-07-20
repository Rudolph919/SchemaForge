namespace SchemaForge.Contracts.V1.Schemas;

public sealed record SchemaVersionDetailResponse(
    Guid Id,
    Guid SchemaDefinitionId,
    string VersionNumber,
    SchemaLifecycleStatus Status,
    string? ChangeSummary,
    DateTimeOffset? PublishedAt,
    SchemaNodeResponse RootNode,
    IReadOnlyList<LocalDefinitionResponse> LocalDefinitions);
