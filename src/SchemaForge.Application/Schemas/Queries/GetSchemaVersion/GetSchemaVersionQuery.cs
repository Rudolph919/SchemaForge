using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaVersion;

public sealed record GetSchemaVersionQuery(Guid SchemaVersionId) : IQuery<Result<SchemaVersionDetail>>;

// Re-exposes SchemaNode/LocalDefinition directly rather than projecting into a parallel
// Application-layer DTO tree - legitimate here (Application depends on Domain), and building a
// second recursive tree shape just to immediately walk it again into a third one (the Contracts
// DTO, which can't reference Domain at all) would be pure duplication with no payoff.
public sealed record SchemaVersionDetail(
    Guid Id,
    Guid SchemaDefinitionId,
    SemVer VersionNumber,
    SchemaLifecycleStatus Status,
    string? ChangeSummary,
    DateTimeOffset? PublishedAt,
    SchemaNode RootNode,
    IReadOnlyList<LocalDefinition> LocalDefinitions,
    uint RowVersion);
