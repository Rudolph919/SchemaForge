using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Components;
using SchemaForge.Domain.Components;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class ComponentVersionRepository(SchemaForgeDbContext dbContext) : IComponentVersionRepository
{
    public Task<ComponentVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.ComponentVersions.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ComponentVersionSummary>> GetAllForComponentDefinitionAsync(
        Guid componentDefinitionId, CancellationToken cancellationToken) =>
        await dbContext.ComponentVersions
            .AsNoTracking()
            .Where(v => v.ComponentDefinitionId == componentDefinitionId)
            .Select(v => new ComponentVersionSummary(v.Id, v.VersionNumber, v.Status, v.ChangeSummary, v.PublishedAt))
            .ToListAsync(cancellationToken);

    public Task<bool> HasDraftAsync(Guid componentDefinitionId, CancellationToken cancellationToken) =>
        dbContext.ComponentVersions.AnyAsync(
            v => v.ComponentDefinitionId == componentDefinitionId && v.Status == SchemaLifecycleStatus.Draft,
            cancellationToken);

    public async Task<SemVer?> GetLatestVersionNumberAsync(Guid componentDefinitionId, CancellationToken cancellationToken)
    {
        var latest = await dbContext.ComponentVersions
            .Where(v => v.ComponentDefinitionId == componentDefinitionId)
            .OrderByDescending(v => v.VersionNumber.Major)
            .ThenByDescending(v => v.VersionNumber.Minor)
            .ThenByDescending(v => v.VersionNumber.Patch)
            .Select(v => new { v.VersionNumber.Major, v.VersionNumber.Minor, v.VersionNumber.Patch })
            .FirstOrDefaultAsync(cancellationToken);

        return latest is null ? null : SemVer.Create(latest.Major, latest.Minor, latest.Patch);
    }

    public async Task AddAsync(ComponentVersion version, CancellationToken cancellationToken) =>
        await dbContext.ComponentVersions.AddAsync(version, cancellationToken);

    public void ApplyExpectedVersion(ComponentVersion version, uint expectedVersion) =>
        dbContext.ApplyExpectedVersion(version, expectedVersion);
}
