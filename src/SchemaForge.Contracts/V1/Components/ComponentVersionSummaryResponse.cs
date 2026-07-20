using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Contracts.V1.Components;

public sealed record ComponentVersionSummaryResponse(
    Guid Id, string VersionNumber, SchemaLifecycleStatus Status, string? ChangeSummary, DateTimeOffset? PublishedAt);
