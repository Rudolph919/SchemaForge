using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Schemas.Commands.PublishSchemaVersion;
using SchemaForge.Domain.Components;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.Domain.Workspaces;
using SchemaForge.Infrastructure.Persistence;
using SchemaForge.Infrastructure.Persistence.Interceptors;
using SchemaForge.Infrastructure.Persistence.Repositories;
using SchemaForge.IntegrationTests.Fixtures;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.IntegrationTests.Application;

// Step 3 §4's cross-aggregate publish invariant, exercised against real Postgres via the actual
// handler + repositories rather than mocked - proves ComponentReferenceValidation actually blocks
// (and later unblocks) a publish, not just that the code compiles. Component controllers don't
// exist yet (that's the next PR), so this drives PublishSchemaVersionHandler directly the way the
// real Api pipeline would.
[Collection(nameof(IntegrationTestCollection))]
public sealed class PublishSchemaVersionReferenceCheckTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FixedTenantContext _tenantContext = null!;
    private DbContextOptions<SchemaForgeDbContext> _options = null!;
    private Guid _organizationId;
    private Guid _schemaDefinitionId;
    private Guid _componentDefinitionId;

    public async Task InitializeAsync()
    {
        _tenantContext = new FixedTenantContext(null);
        _options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor(), new TenantSessionConnectionInterceptor(_tenantContext))
            .Options;

        var organization = Organization.Create("Verify ComponentRef Co", Slug.Create($"verify-component-ref-{Guid.NewGuid():N}"));
        _organizationId = organization.Id;
        _tenantContext.SetTenant(organization.Id);

        var project = Project.Create(organization.Id, "Verify Project");
        var schemaDefinition = SchemaDefinition.Create(organization.Id, project.Id, "Invoice Schema");
        _schemaDefinitionId = schemaDefinition.Id;
        var componentDefinition = ComponentDefinition.Create(organization.Id, "PostalAddress");
        _componentDefinitionId = componentDefinition.Id;

        await using var context = new SchemaForgeDbContext(_options, _tenantContext);
        context.Organizations.Add(organization);
        context.Projects.Add(project);
        context.SchemaDefinitions.Add(schemaDefinition);
        context.ComponentDefinitions.Add(componentDefinition);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Publish_fails_while_a_referenced_component_is_still_Draft()
    {
        var componentVersion = ComponentVersion.CreateDraft(_organizationId, _componentDefinitionId, SemVer.Initial);
        var schemaVersion = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Initial);
        var addressId = schemaVersion.AddObjectProperty(schemaVersion.RootNode.Id, "billingAddress", NodeKind.Object).Value;
        schemaVersion.UpdateNode(addressId, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            ComponentReference = new ComponentReference(componentVersion.Id, VersionConstraint.Latest),
        });

        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.ComponentVersions.Add(componentVersion);
            writeContext.SchemaVersions.Add(schemaVersion);
            await writeContext.SaveChangesAsync();
        }

        await using var handlerContext = new SchemaForgeDbContext(_options, _tenantContext);
        var handler = new PublishSchemaVersionHandler(
            new SchemaVersionRepository(handlerContext), new ComponentVersionRepository(handlerContext));

        var result = await handler.Handle(new PublishSchemaVersionCommand(schemaVersion.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComponentReference.NotPublished");
    }

    [Fact]
    public async Task Publish_succeeds_once_the_referenced_component_is_Published()
    {
        var componentVersion = ComponentVersion.CreateDraft(_organizationId, _componentDefinitionId, SemVer.Initial);
        componentVersion.Publish();
        var schemaVersion = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Initial);
        var addressId = schemaVersion.AddObjectProperty(schemaVersion.RootNode.Id, "billingAddress", NodeKind.Object).Value;
        schemaVersion.UpdateNode(addressId, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            ComponentReference = new ComponentReference(componentVersion.Id, VersionConstraint.Latest),
        });

        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.ComponentVersions.Add(componentVersion);
            writeContext.SchemaVersions.Add(schemaVersion);
            await writeContext.SaveChangesAsync();
        }

        await using var handlerContext = new SchemaForgeDbContext(_options, _tenantContext);
        var handler = new PublishSchemaVersionHandler(
            new SchemaVersionRepository(handlerContext), new ComponentVersionRepository(handlerContext));

        var result = await handler.Handle(new PublishSchemaVersionCommand(schemaVersion.Id), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        // Handlers stage changes via the change tracker only - TransactionBehavior is the sole
        // place SaveChangesAsync is normally called (Step 1 §3's pipeline), which this direct
        // handler.Handle() call bypasses entirely. Missing this the first time round made the
        // test pass its own IsSuccess assertion while silently persisting nothing.
        await handlerContext.SaveChangesAsync();

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var reloaded = await readContext.SchemaVersions.SingleAsync(v => v.Id == schemaVersion.Id);
        reloaded.Status.Should().Be(SchemaLifecycleStatus.Published);
    }

    private sealed class FixedTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; } = tenantId;

        public void SetTenant(Guid organizationId) => CurrentTenantId = organizationId;
    }
}
