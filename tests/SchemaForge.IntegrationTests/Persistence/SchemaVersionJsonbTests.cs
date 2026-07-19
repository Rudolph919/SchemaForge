using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.Domain.Workspaces;
using SchemaForge.Infrastructure.Persistence;
using SchemaForge.Infrastructure.Persistence.Interceptors;
using SchemaForge.IntegrationTests.Fixtures;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.IntegrationTests.Persistence;

// SchemaVersion.RootNode/LocalDefinitions are persisted as jsonb via a hand-written value
// converter, not EF Core's native owned-type JSON mapping - that mapping can't represent a
// genuinely self-referential recursive structure (confirmed with a throwaway script before this
// was built: configuring it recurses infinitely at model-building time). This is the single
// riskiest technical bet in the whole schema-design core (Step 5 §2), so it gets its own
// dedicated, lasting regression test here rather than only a one-off verification script.
[Collection(nameof(IntegrationTestCollection))]
public sealed class SchemaVersionJsonbTests(PostgresFixture postgres) : IAsyncLifetime
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

        var organization = Organization.Create("Verify JSONB Co", Slug.Create($"verify-jsonb-{Guid.NewGuid():N}"));
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
    public async Task A_deeply_nested_tree_with_every_field_populated_round_trips_exactly()
    {
        var version = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Initial, "Initial draft");

        var invoiceNumberId = version.AddObjectProperty(version.RootNode.Id, "invoiceNumber", NodeKind.String).Value;
        version.UpdateNode(invoiceNumberId, new SchemaNodeContent(
            NodeKind.String, "The invoice's unique number", "internal note", false, true,
            [JsonLiteral.FromRawJson("\"INV-0001\"")], JsonLiteral.FromRawJson("\"INV-0000\""),
            [JsonLiteral.FromRawJson("\"INV-0001\""), JsonLiteral.FromRawJson("\"INV-0002\"")], null,
            null, null, new StringConstraints(3, 20, "^INV-[0-9]+$", SchemaFormat.Custom, "invoice-number"), null,
            null, null, null, null));

        var lineItemsId = version.AddObjectProperty(version.RootNode.Id, "lineItems", NodeKind.Array).Value;
        var itemsNodeId = version.SetArrayItemsNode(lineItemsId, NodeKind.Object).Value;
        version.AddObjectProperty(itemsNodeId, "sku", NodeKind.String);
        version.UpdateNode(itemsNodeId, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            DependentRequired = new Dictionary<string, IReadOnlyList<string>> { ["sku"] = ["quantity"] },
        });

        var payerId = version.AddObjectProperty(version.RootNode.Id, "payer", null).Value;
        version.UpdateNode(payerId, SchemaNodeContent.Empty(null) with { Composition = CompositionKind.OneOf });
        version.AddCompositionBranch(payerId, NodeKind.Object);
        version.AddCompositionBranch(payerId, NodeKind.Object);

        var componentVersionId = Guid.NewGuid();
        var addressId = version.AddObjectProperty(version.RootNode.Id, "billingAddress", NodeKind.Object).Value;
        version.UpdateNode(addressId, SchemaNodeContent.Empty(NodeKind.Object) with
        {
            ComponentReference = new ComponentReference(componentVersionId, VersionConstraint.ExactVersion(SemVer.Create(1, 2, 0))),
        });

        var localDefinitionId = version.AddLocalDefinition("Category", NodeKind.Object).Value;
        var localDefinitionRoot = version.LocalDefinitions.Single(d => d.Id == localDefinitionId).RootNode;
        var subcategoriesId = version.AddObjectProperty(localDefinitionRoot.Id, "subcategories", NodeKind.Array).Value;
        version.UpdateNode(subcategoriesId, SchemaNodeContent.Empty(NodeKind.Array) with { LocalDefinitionRef = localDefinitionId });

        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.SchemaVersions.Add(version);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var reloaded = await readContext.SchemaVersions.SingleAsync(v => v.Id == version.Id);

        reloaded.VersionNumber.Should().Be(SemVer.Initial);
        reloaded.RootNode.Properties.Should().HaveCount(4);

        var invoiceNumber = reloaded.RootNode.Properties.Single(p => p.PropertyName == "invoiceNumber");
        invoiceNumber.Description.Should().Be("The invoice's unique number");
        invoiceNumber.AllowedValues.Should().HaveCount(2);
        invoiceNumber.StringConstraints!.Pattern.Should().Be("^INV-[0-9]+$");
        invoiceNumber.StringConstraints.CustomFormatValue.Should().Be("invoice-number");

        var lineItems = reloaded.RootNode.Properties.Single(p => p.PropertyName == "lineItems");
        lineItems.ItemsNode!.DependentRequired!["sku"].Should().ContainSingle().Which.Should().Be("quantity");

        var payer = reloaded.RootNode.Properties.Single(p => p.PropertyName == "payer");
        payer.Composition.Should().Be(CompositionKind.OneOf);
        payer.CompositionBranches.Should().HaveCount(2);

        var billingAddress = reloaded.RootNode.Properties.Single(p => p.PropertyName == "billingAddress");
        billingAddress.ComponentReference!.ComponentVersionId.Should().Be(componentVersionId);
        billingAddress.ComponentReference.Constraint.Version.Should().Be(SemVer.Create(1, 2, 0));

        reloaded.LocalDefinitions.Should().ContainSingle();
        var reloadedLocalDefinition = reloaded.LocalDefinitions.Single();
        var reloadedSubcategories = reloadedLocalDefinition.RootNode.Properties.Single(p => p.PropertyName == "subcategories");
        reloadedSubcategories.LocalDefinitionRef.Should().Be(reloadedLocalDefinition.Id);
    }

    [Fact]
    public async Task An_in_place_tree_mutation_is_detected_and_persisted_on_the_next_save()
    {
        var version = SchemaVersion.CreateDraft(_organizationId, _schemaDefinitionId, SemVer.Initial);
        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            writeContext.SchemaVersions.Add(version);
            await writeContext.SaveChangesAsync();
        }

        int affectedRows;
        await using (var mutateContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            var loaded = await mutateContext.SchemaVersions.SingleAsync(v => v.Id == version.Id);
            loaded.AddObjectProperty(loaded.RootNode.Id, "amount", NodeKind.Number);
            affectedRows = await mutateContext.SaveChangesAsync();
        }

        // Without a correct ValueComparer, EF's default reference-equality change detection
        // would never notice this in-place edit and would silently skip writing it - this is
        // exactly the failure mode the comparer in SchemaVersionConfiguration exists to prevent.
        affectedRows.Should().Be(1);

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var reloaded = await readContext.SchemaVersions.SingleAsync(v => v.Id == version.Id);
        reloaded.RootNode.Properties.Should().ContainSingle(p => p.PropertyName == "amount");
    }

    private sealed class FixedTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; } = tenantId;

        public void SetTenant(Guid organizationId) => CurrentTenantId = organizationId;
    }
}
