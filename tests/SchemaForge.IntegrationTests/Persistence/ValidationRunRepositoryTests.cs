using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.Domain.Validation;
using SchemaForge.Domain.Workspaces;
using SchemaForge.Infrastructure.Persistence;
using SchemaForge.Infrastructure.Persistence.Interceptors;
using SchemaForge.Infrastructure.Persistence.Repositories;
using SchemaForge.IntegrationTests.Fixtures;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.IntegrationTests.Persistence;

// ValidationError.Path is mapped as a value-converted scalar, not a nested OwnsOne - EF's
// constructor binding for ValidationError's positional-record constructor couldn't bind a
// nested owned navigation as a parameter (confirmed live: "no suitable constructor found" at
// migration-design time). This proves the fix actually round-trips through real Postgres.
[Collection(nameof(IntegrationTestCollection))]
public sealed class ValidationRunRepositoryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FixedTenantContext _tenantContext = null!;
    private DbContextOptions<SchemaForgeDbContext> _options = null!;
    private Guid _organizationId;
    private Guid _projectId;
    private Guid _schemaVersionId;
    private Guid _executedByUserId;

    public async Task InitializeAsync()
    {
        _tenantContext = new FixedTenantContext(null);
        _options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor(), new TenantSessionConnectionInterceptor(_tenantContext))
            .Options;

        var organization = Organization.Create("Verify ValidationRun Co", Slug.Create($"verify-vr-{Guid.NewGuid():N}"));
        _organizationId = organization.Id;
        _tenantContext.SetTenant(organization.Id);

        var project = Project.Create(organization.Id, "Verify Project");
        _projectId = project.Id;
        var schemaDefinition = SchemaDefinition.Create(organization.Id, project.Id, "Invoice Schema");
        var version = SchemaVersion.CreateDraft(organization.Id, schemaDefinition.Id, SemVer.Initial);
        _schemaVersionId = version.Id;

        // ExecutedByUserId is a real, non-nullable foreign key (unlike CreatedByUserId) - needs
        // an actual persisted user row, not just any Guid.
        var user = User.Register(
            EmailAddress.Create($"validator-{Guid.NewGuid():N}@example.com"), "hash", "Validator");
        _executedByUserId = user.Id;

        await using var context = new SchemaForgeDbContext(_options, _tenantContext);
        context.Organizations.Add(organization);
        context.Users.Add(user);
        context.Projects.Add(project);
        context.SchemaDefinitions.Add(schemaDefinition);
        context.SchemaVersions.Add(version);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_run_with_multiple_errors_round_trips_including_each_errors_path()
    {
        var errors = new[]
        {
            new ValidationError(JsonPath.Root.AppendProperty("name"), "object.required-property-missing", "Missing 'name'.", ErrorSeverity.Error),
            new ValidationError(JsonPath.Root.AppendProperty("items").AppendIndex(0), "type.mismatch", "Wrong type.", ErrorSeverity.Error),
        };
        var run = ValidationRun.Record(_organizationId, _projectId, _schemaVersionId, "deadbeef", errors, _executedByUserId);

        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            var repository = new ValidationRunRepository(writeContext);
            await repository.AddAsync(run, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var reloadedRepository = new ValidationRunRepository(readContext);
        var reloaded = await reloadedRepository.GetAllForSchemaVersionAsync(_schemaVersionId, CancellationToken.None);

        reloaded.Should().ContainSingle();
        var reloadedRun = reloaded.Single();
        reloadedRun.Outcome.Should().Be(ValidationOutcome.Invalid);
        reloadedRun.InputPayloadHash.Should().Be("deadbeef");
        reloadedRun.Errors.Should().HaveCount(2);
        reloadedRun.Errors.Should().Contain(e => e.Path.Value == "$.name" && e.Code == "object.required-property-missing");
        reloadedRun.Errors.Should().Contain(e => e.Path.Value == "$.items[0]" && e.Code == "type.mismatch");
    }

    [Fact]
    public async Task A_run_with_no_errors_is_valid_and_round_trips_an_empty_error_list()
    {
        var run = ValidationRun.Record(_organizationId, _projectId, _schemaVersionId, "cafebabe", [], _executedByUserId);

        await using (var writeContext = new SchemaForgeDbContext(_options, _tenantContext))
        {
            var repository = new ValidationRunRepository(writeContext);
            await repository.AddAsync(run, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new SchemaForgeDbContext(_options, _tenantContext);
        var reloadedRepository = new ValidationRunRepository(readContext);
        var reloaded = (await reloadedRepository.GetAllForSchemaVersionAsync(_schemaVersionId, CancellationToken.None)).Single();

        reloaded.Outcome.Should().Be(ValidationOutcome.Valid);
        reloaded.Errors.Should().BeEmpty();
    }

    private sealed class FixedTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; } = tenantId;

        public void SetTenant(Guid organizationId) => CurrentTenantId = organizationId;
    }
}
