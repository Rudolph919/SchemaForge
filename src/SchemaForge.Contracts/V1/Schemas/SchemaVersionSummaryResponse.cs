namespace SchemaForge.Contracts.V1.Schemas;

public sealed record SchemaVersionSummaryResponse(
    Guid Id, string VersionNumber, SchemaLifecycleStatus Status, string? ChangeSummary, DateTimeOffset? PublishedAt);
