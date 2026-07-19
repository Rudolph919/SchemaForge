using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class SchemaVersionRepository(SchemaForgeDbContext dbContext) : ISchemaVersionRepository
{
    public Task<SchemaVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SchemaVersions.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    // Projects straight onto SchemaVersionSummary without ever touching root_node/local_definitions
    // - EF only selects the columns the projection actually needs, so this never pays the jsonb
    // deserialize cost (Step 6 §2.4's "headers only" listing). AsNoTracking is required, not
    // just a nice-to-have: EF refuses to track a projected owned-type instance (VersionNumber)
    // without also tracking its owning entity, and this query deliberately doesn't select the
    // owner - confirmed live, this throws InvalidOperationException without it.
    public async Task<IReadOnlyList<SchemaVersionSummary>> GetAllForSchemaDefinitionAsync(
        Guid schemaDefinitionId, CancellationToken cancellationToken) =>
        await dbContext.SchemaVersions
            .AsNoTracking()
            .Where(v => v.SchemaDefinitionId == schemaDefinitionId)
            .Select(v => new SchemaVersionSummary(v.Id, v.VersionNumber, v.Status, v.ChangeSummary, v.PublishedAt))
            .ToListAsync(cancellationToken);

    public Task<bool> HasDraftAsync(Guid schemaDefinitionId, CancellationToken cancellationToken) =>
        dbContext.SchemaVersions.AnyAsync(
            v => v.SchemaDefinitionId == schemaDefinitionId && v.Status == SchemaLifecycleStatus.Draft,
            cancellationToken);

    public async Task<SemVer?> GetLatestVersionNumberAsync(Guid schemaDefinitionId, CancellationToken cancellationToken)
    {
        var latest = await dbContext.SchemaVersions
            .Where(v => v.SchemaDefinitionId == schemaDefinitionId)
            .OrderByDescending(v => v.VersionNumber.Major)
            .ThenByDescending(v => v.VersionNumber.Minor)
            .ThenByDescending(v => v.VersionNumber.Patch)
            .Select(v => new { v.VersionNumber.Major, v.VersionNumber.Minor, v.VersionNumber.Patch })
            .FirstOrDefaultAsync(cancellationToken);

        return latest is null ? null : SemVer.Create(latest.Major, latest.Minor, latest.Patch);
    }

    public async Task AddAsync(SchemaVersion version, CancellationToken cancellationToken) =>
        await dbContext.SchemaVersions.AddAsync(version, cancellationToken);
}
