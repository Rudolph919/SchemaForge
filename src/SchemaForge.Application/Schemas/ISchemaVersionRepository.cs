using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas;

public interface ISchemaVersionRepository
{
    // Full aggregate, node tree included - the one load that pays the JSONB deserialize cost
    // (Step 6 §2.4), used by every command handler that needs to mutate a version and by the
    // "get one version" query.
    Task<SchemaVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Headers only, no node tree - a lean projection (Step 1 §3) so listing a schema's version
    // history never pays the JSONB deserialize cost for versions the caller isn't opening.
    Task<IReadOnlyList<SchemaVersionSummary>> GetAllForSchemaDefinitionAsync(
        Guid schemaDefinitionId, CancellationToken cancellationToken);

    Task<bool> HasDraftAsync(Guid schemaDefinitionId, CancellationToken cancellationToken);

    Task<SemVer?> GetLatestVersionNumberAsync(Guid schemaDefinitionId, CancellationToken cancellationToken);

    Task AddAsync(SchemaVersion version, CancellationToken cancellationToken);

    void ApplyExpectedVersion(SchemaVersion version, uint expectedVersion);
}

public sealed record SchemaVersionSummary(
    Guid Id, SemVer VersionNumber, SchemaLifecycleStatus Status, string? ChangeSummary, DateTimeOffset? PublishedAt);
