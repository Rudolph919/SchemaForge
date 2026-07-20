using SchemaForge.Domain.Components;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Components;

public interface IComponentVersionRepository
{
    Task<ComponentVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ComponentVersionSummary>> GetAllForComponentDefinitionAsync(
        Guid componentDefinitionId, CancellationToken cancellationToken);

    Task<bool> HasDraftAsync(Guid componentDefinitionId, CancellationToken cancellationToken);

    Task<SemVer?> GetLatestVersionNumberAsync(Guid componentDefinitionId, CancellationToken cancellationToken);

    Task AddAsync(ComponentVersion version, CancellationToken cancellationToken);
}

public sealed record ComponentVersionSummary(
    Guid Id, SemVer VersionNumber, SchemaLifecycleStatus Status, string? ChangeSummary, DateTimeOffset? PublishedAt);
