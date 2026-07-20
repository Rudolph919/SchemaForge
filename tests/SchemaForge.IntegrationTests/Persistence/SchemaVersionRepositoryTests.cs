using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Workspaces;
using SchemaForge.Infrastructure.Persistence;
using SchemaForge.Infrastructure.Persistence.Interceptors;
using SchemaForge.Infrastructure.Persistence.Repositories;
using SchemaForge.IntegrationTests.Fixtures;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.IntegrationTests.Persistence;

// Exercises two LINQ patterns that don't exist anywhere else in this codebase yet, so their SQL
// translation needed proving against real Postgres, not just a compile-time check: projecting a
// whole owned value object (SemVer) directly into a DTO constructor, and ordering by an owned
// type's individual properties (Major/Minor/Patch) to find "the latest version."
[Collection(nameof(IntegrationTestCollection))]
public sealed class SchemaVersionRepositoryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FixedTenantContext _tenantContext = null!;
    private DbContextOptions<SchemaForgeDbContext> _options = null!;
    private Guid _organizationId;
    private Guid _schemaDefinitionId;

    public async Task InitializeAsync()
    {
        _tenantContext = new FixedTenantContext(null);
        _options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor(), new TenantSessionConnectionInterceptor(_tenantContext))
            .Options;

        var organization = Organization.Create("Verify Repo Co", Slug.Create($"verify-repo-{Guid.NewGuid():N}"));
        _organizationId = organization.Id;
        _tenantContext.SetTenant(organization.Id);

        var project = Project.Create(organization.Id, "Verify Project");
        var schemaDefinition = SchemaDefinition.Create(organization.Id, project.Id, "Invoice Schema");
        _schemaDefinitionId = schemaDefinition.Id;

        await using var context = new SchemaForgeDbContext(_options, _tenantContext);
        context.Organizations.Add(organization);
        context.Projects.Add(project);
        context.SchemaDefinitions.Add(schemaDefinition);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAllForSchemaDefinitionAsync_projects_headers_without_the_node_tree()
    {
        var version = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Initial, "First cut");
        version.AddObjectProperty(version.RootNode.Id, "amount", NodeKind.Number);

        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.SchemaVersions.Add(version);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var repository = new SchemaVersionRepository(readContext);

        var summaries = await repository.GetAllForSchemaDefinitionAsync(_schemaDefinitionId, CancellationToken.None);

        summaries.Should().ContainSingle();
        var summary = summaries.Single();
        summary.Id.Should().Be(version.Id);
        summary.VersionNumber.Should().Be(SemVer.Initial);
        summary.Status.Should().Be(SchemaLifecycleStatus.Draft);
        summary.ChangeSummary.Should().Be("First cut");
    }

    [Fact]
    public async Task GetLatestVersionNumberAsync_returns_null_when_no_versions_exist()
    {
        await using var context = new SchemaForgeDbContext(_options, _tenantContext);
        var repository = new SchemaVersionRepository(context);

        var latest = await repository.GetLatestVersionNumberAsync(_schemaDefinitionId, CancellationToken.None);

        latest.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestVersionNumberAsync_finds_the_highest_version_by_semver_ordering_not_creation_order()
    {
        // Published in an order that would give the wrong answer under naive
        // most-recently-created ordering - 1.2.0 is the highest version even though it wasn't
        // the last one created.
        var v100 = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Create(1, 0, 0));
        v100.Publish();
        var v120 = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Create(1, 2, 0));
        v120.Publish();
        var v110 = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Create(1, 1, 0));
        v110.Publish();

        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.SchemaVersions.AddRange(v100, v120, v110);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var repository = new SchemaVersionRepository(readContext);

        var latest = await repository.GetLatestVersionNumberAsync(_schemaDefinitionId, CancellationToken.None);

        latest.Should().Be(SemVer.Create(1, 2, 0));
    }

    // HasDraftAsync (used by CreateSchemaVersionHandler) is a check-then-act guard, not an atomic
    // one - it can't stop two concurrent requests that both read "no draft exists" before either
    // writes. The partial unique index (`ux_schema_versions_one_draft ... WHERE status = 'Draft'`,
    // Step 3 §4/Step 5 §2) is what actually makes the invariant hold under a race, at the database
    // level rather than the application level. This bypasses HasDraftAsync entirely to prove that.
    [Fact]
    public async Task The_partial_unique_index_rejects_a_second_concurrently_inserted_draft()
    {
        var first = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Initial);
        var second = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Create(1, 1, 0));

        await using var firstContext = new SchemaForgeDbContext(_options, _tenantContext);
        firstContext.SchemaVersions.Add(first);
        await firstContext.SaveChangesAsync();

        await using var secondContext = new SchemaForgeDbContext(_options, _tenantContext);
        secondContext.SchemaVersions.Add(second);
        var act = () => secondContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task HasDraftAsync_reflects_the_current_draft_state()
    {
        await using (var context = new SchemaForgeDbContext(_options, _tenantContext))
        {
            var repository = new SchemaVersionRepository(context);
            (await repository.HasDraftAsync(_schemaDefinitionId, CancellationToken.None)).Should().BeFalse();
        }

        var version = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Initial);
        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.SchemaVersions.Add(version);
            await writeContext.SaveChangesAsync();
        }

        await using var afterContext = new SchemaForgeDbContext(_options, _tenantContext);
        var afterRepository = new SchemaVersionRepository(afterContext);
        (await afterRepository.HasDraftAsync(_schemaDefinitionId, CancellationToken.None)).Should().BeTrue();
    }

    private sealed class FixedTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; } = tenantId;

        public void SetTenant(Guid organizationId) => CurrentTenantId = organizationId;
    }
}
