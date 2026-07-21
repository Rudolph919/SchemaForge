using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Components.Queries.GetComponentVersion;

public sealed record GetComponentVersionQuery(Guid ComponentVersionId) : IQuery<Result<ComponentVersionDetail>>;

// Re-exposes SchemaNode/LocalDefinition directly, same reasoning as SchemaVersionDetail
// (GetSchemaVersionQuery) - Application depends on Domain, and building a parallel tree shape
// just to immediately walk it into a third one (the Contracts DTO) would be pure duplication.
public sealed record ComponentVersionDetail(
    Guid Id,
    Guid ComponentDefinitionId,
    SemVer VersionNumber,
    SchemaLifecycleStatus Status,
    string? ChangeSummary,
    DateTimeOffset? PublishedAt,
    SchemaNode RootNode,
    IReadOnlyList<LocalDefinition> LocalDefinitions,
    uint RowVersion);
