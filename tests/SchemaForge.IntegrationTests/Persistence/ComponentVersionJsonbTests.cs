using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Components;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.Infrastructure.Persistence;
using SchemaForge.Infrastructure.Persistence.Interceptors;
using SchemaForge.IntegrationTests.Fixtures;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.IntegrationTests.Persistence;

// ComponentVersionConfiguration reuses SchemaNodeJsonConverter/its ValueComparer verbatim (Step 7
// §3) rather than duplicating the jsonb wiring - this proves that reuse actually works for a
// second, independent aggregate/table, not just that it compiles. Deliberately lighter than
// SchemaVersionJsonbTests (which already proved the converter itself against every field) - the
// converter isn't new here, only its application to a second EF configuration is.
[Collection(nameof(IntegrationTestCollection))]
public sealed class ComponentVersionJsonbTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FixedTenantContext _tenantContext = null!;
    private DbContextOptions<SchemaForgeDbContext> _options = null!;
    private Guid _organizationId;
    private Guid _componentDefinitionId;

    public async Task InitializeAsync()
    {
        _tenantContext = new FixedTenantContext(null);
        _options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor(), new TenantSessionConnectionInterceptor(_tenantContext))
            .Options;

        var organization = Organization.Create("Verify Component JSONB Co", Slug.Create($"verify-component-jsonb-{Guid.NewGuid():N}"));
        _organizationId = organization.Id;
        _tenantContext.SetTenant(organization.Id);

        var componentDefinition = ComponentDefinition.Create(organization.Id, "PostalAddress");
        _componentDefinitionId = componentDefinition.Id;

        await using var context = new SchemaForgeDbContext(_options, _tenantContext);
        context.Organizations.Add(organization);
        context.ComponentDefinitions.Add(componentDefinition);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_nested_tree_with_a_local_definition_round_trips_exactly()
    {
        var version = ComponentVersion.CreateDraft(_organizationId, _componentDefinitionId, SemVer.Initial, "Initial draft");

        var streetId = version.AddObjectProperty(version.RootNode.Id, "street", NodeKind.String).Value;
        version.UpdateNode(streetId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            IsRequiredByParent = true,
            StringConstraints = new StringConstraints(1, 200, null, null, null),
        });

        var localDefinitionId = version.AddLocalDefinition("Country", NodeKind.String).Value;
        var countryId = version.AddObjectProperty(version.RootNode.Id, "country", NodeKind.String).Value;
        version.UpdateNode(countryId, SchemaNodeContent.Empty(NodeKind.String) with { LocalDefinitionRef = localDefinitionId });

        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.ComponentVersions.Add(version);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var reloaded = await readContext.ComponentVersions.SingleAsync(v => v.Id == version.Id);

        reloaded.VersionNumber.Should().Be(SemVer.Initial);
        reloaded.RootNode.Properties.Should().HaveCount(2);

        var street = reloaded.RootNode.Properties.Single(p => p.PropertyName == "street");
        street.IsRequiredByParent.Should().BeTrue();
        street.StringConstraints!.MaxLength.Should().Be(200);

        var country = reloaded.RootNode.Properties.Single(p => p.PropertyName == "country");
        reloaded.LocalDefinitions.Should().ContainSingle();
        country.LocalDefinitionRef.Should().Be(reloaded.LocalDefinitions.Single().Id);
    }

    [Fact]
    public async Task An_in_place_tree_mutation_is_detected_and_persisted_on_the_next_save()
    {
        var version = ComponentVersion.CreateDraft(_organizationId, _componentDefinitionId, SemVer.Initial);
        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.ComponentVersions.Add(version);
            await writeContext.SaveChangesAsync();
        }

        int affectedRows;
        await using (var mutateContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            var loaded = await mutateContext.ComponentVersions.SingleAsync(v => v.Id == version.Id);
            loaded.AddObjectProperty(loaded.RootNode.Id, "city", NodeKind.String);
            affectedRows = await mutateContext.SaveChangesAsync();
        }

        affectedRows.Should().Be(1);

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var reloaded = await readContext.ComponentVersions.SingleAsync(v => v.Id == version.Id);
        reloaded.RootNode.Properties.Should().ContainSingle(p => p.PropertyName == "city");
    }

    private sealed class FixedTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; } = tenantId;

        public void SetTenant(Guid organizationId) => CurrentTenantId = organizationId;
    }
}
