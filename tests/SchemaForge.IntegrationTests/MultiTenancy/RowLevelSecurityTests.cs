using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Validation;
using SchemaForge.Domain.Workspaces;
using SchemaForge.Infrastructure.Persistence;
using SchemaForge.Infrastructure.Persistence.Interceptors;
using SchemaForge.IntegrationTests.Fixtures;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.IntegrationTests.MultiTenancy;

// Asserts the Postgres RLS layer independently of the EF Core query filter (Step 5 §3, Step 7
// §5) - every test here deliberately constructs the "EF filter would have failed to protect
// this" scenario, so a passing suite proves RLS alone is what's actually stopping the leak, not
// just that the two layers happen to agree.
[Collection(nameof(IntegrationTestCollection))]
public sealed class RowLevelSecurityTests(PostgresFixture postgres) : IAsyncLifetime
{
    private SchemaForgeApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SchemaForgeApiFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task RLS_blocks_a_cross_tenant_read_even_with_the_EF_Core_filter_explicitly_bypassed()
    {
        var orgA = await RegisterAsync("Org A");
        var orgB = await RegisterAsync("Org B");

        var tenantContext = new FixedTenantContext(orgA.OrganizationId);

        // Same as QueryFilterIsolationTests: a manually constructed DbContext needs the same
        // TenantSessionConnectionInterceptor the real app attaches via DI, or RLS blocks
        // everything (even the tenant's own data), not just the cross-tenant case under test.
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;
        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);

        // IgnoreQueryFilters() deliberately removes the EF Core layer's protection entirely -
        // simulating a future forgotten filter or a hand-written query bug. If RLS is the only
        // thing left standing, Org B's row must still be unreachable.
        var visible = await dbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .Select(m => m.OrganizationId)
            .ToListAsync();

        visible.Should().ContainSingle(m => m == orgA.OrganizationId);
        visible.Should().NotContain(orgB.OrganizationId);
    }

    [Fact]
    public async Task RLS_blocks_a_raw_SQL_write_to_another_organizations_row_with_no_EF_Core_involved_at_all()
    {
        var orgA = await RegisterAsync("Org A");
        var orgB = await RegisterAsync("Org B");

        await using var connection = new NpgsqlConnection(postgres.AppConnectionString);
        await connection.OpenAsync();
        await SetTenantSessionVariableAsync(connection, orgA.OrganizationId);

        await using var updateAttempt = connection.CreateCommand();
        updateAttempt.CommandText =
            "UPDATE organization_memberships SET role = 'Admin' WHERE organization_id = @orgB";
        updateAttempt.Parameters.AddWithValue("orgB", orgB.OrganizationId);

        // Not an exception - RLS's USING clause makes Org B's row invisible to this connection,
        // so the UPDATE simply matches zero rows, the same as if the row didn't exist.
        var rowsAffected = await updateAttempt.ExecuteNonQueryAsync();

        rowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task RLS_blocks_a_cross_tenant_project_read_even_with_the_EF_Core_filter_explicitly_bypassed()
    {
        var orgA = await RegisterAsync("Org A");
        var orgB = await RegisterAsync("Org B");

        await CreateProjectAsync(orgA.OrganizationId, "Org A's Project");

        var tenantContext = new FixedTenantContext(orgB.OrganizationId);
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;
        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);

        var visible = await dbContext.Projects.IgnoreQueryFilters().Select(p => p.OrganizationId).ToListAsync();

        visible.Should().NotContain(orgA.OrganizationId);
    }

    // schema_definitions/schema_versions/validation_runs all got the same hand-added RLS policy
    // SQL as the Phase 1 tables (Step 5 §3) when their migrations were written - this proves that
    // policy actually exists and works for a real cross-tenant read on each of the three new
    // tables, not just that the migration file contains the right-looking SQL.
    [Fact]
    public async Task RLS_blocks_a_cross_tenant_schema_definition_read_even_with_the_EF_Core_filter_explicitly_bypassed()
    {
        var orgA = await RegisterAsync("Org A");
        var orgB = await RegisterAsync("Org B");

        var projectId = await CreateProjectAsync(orgA.OrganizationId, "Org A's Project");
        await CreateSchemaDefinitionAsync(orgA.OrganizationId, projectId, "Org A's Schema");

        var tenantContext = new FixedTenantContext(orgB.OrganizationId);
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;
        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);

        var visible = await dbContext.SchemaDefinitions.IgnoreQueryFilters().Select(d => d.OrganizationId).ToListAsync();

        visible.Should().NotContain(orgA.OrganizationId);
    }

    [Fact]
    public async Task RLS_blocks_a_cross_tenant_schema_version_read_even_with_the_EF_Core_filter_explicitly_bypassed()
    {
        var orgA = await RegisterAsync("Org A");
        var orgB = await RegisterAsync("Org B");

        var projectId = await CreateProjectAsync(orgA.OrganizationId, "Org A's Project");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(orgA.OrganizationId, projectId, "Org A's Schema");
        await CreateDraftVersionAsync(orgA.OrganizationId, schemaDefinitionId);

        var tenantContext = new FixedTenantContext(orgB.OrganizationId);
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;
        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);

        var visible = await dbContext.SchemaVersions.IgnoreQueryFilters().Select(v => v.OrganizationId).ToListAsync();

        visible.Should().NotContain(orgA.OrganizationId);
    }

    [Fact]
    public async Task RLS_blocks_a_cross_tenant_validation_run_read_even_with_the_EF_Core_filter_explicitly_bypassed()
    {
        var orgA = await RegisterAsync("Org A");
        var orgB = await RegisterAsync("Org B");

        var projectId = await CreateProjectAsync(orgA.OrganizationId, "Org A's Project");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(orgA.OrganizationId, projectId, "Org A's Schema");
        var versionId = await CreateDraftVersionAsync(orgA.OrganizationId, schemaDefinitionId);
        await RecordValidationRunAsync(orgA.OrganizationId, projectId, versionId, orgA.UserId);

        var tenantContext = new FixedTenantContext(orgB.OrganizationId);
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;
        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);

        var visible = await dbContext.ValidationRuns.IgnoreQueryFilters().Select(r => r.OrganizationId).ToListAsync();

        visible.Should().NotContain(orgA.OrganizationId);
    }

    [Fact]
    public async Task RLS_allows_a_raw_SQL_write_to_the_matching_tenants_own_row()
    {
        var orgA = await RegisterAsync("Org A");

        await using var connection = new NpgsqlConnection(postgres.AppConnectionString);
        await connection.OpenAsync();
        await SetTenantSessionVariableAsync(connection, orgA.OrganizationId);

        await using var updateAttempt = connection.CreateCommand();
        updateAttempt.CommandText =
            "UPDATE organization_memberships SET role = 'Admin' WHERE organization_id = @orgA";
        updateAttempt.Parameters.AddWithValue("orgA", orgA.OrganizationId);

        var rowsAffected = await updateAttempt.ExecuteNonQueryAsync();

        rowsAffected.Should().Be(1);
    }

    private async Task<RegisterResponse> RegisterAsync(string organizationName)
    {
        var request = new RegisterRequest(
            $"{Guid.NewGuid()}@example.com", "correct-horse-battery", "Test User", organizationName);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    private async Task<Guid> CreateProjectAsync(Guid organizationId, string name)
    {
        var tenantContext = new FixedTenantContext(organizationId);
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;

        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);
        var project = Project.Create(organizationId, name);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();
        return project.Id;
    }

    private async Task<Guid> CreateSchemaDefinitionAsync(Guid organizationId, Guid projectId, string name)
    {
        var tenantContext = new FixedTenantContext(organizationId);
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;

        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);
        var schemaDefinition = SchemaDefinition.Create(organizationId, projectId, name);
        dbContext.SchemaDefinitions.Add(schemaDefinition);
        await dbContext.SaveChangesAsync();
        return schemaDefinition.Id;
    }

    private async Task<Guid> CreateDraftVersionAsync(Guid organizationId, Guid schemaDefinitionId)
    {
        var tenantContext = new FixedTenantContext(organizationId);
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;

        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);
        var version = SchemaVersion.CreateDraft(organizationId, schemaDefinitionId, SemVer.Initial);
        dbContext.SchemaVersions.Add(version);
        await dbContext.SaveChangesAsync();
        return version.Id;
    }

    private async Task RecordValidationRunAsync(Guid organizationId, Guid projectId, Guid schemaVersionId, Guid executedByUserId)
    {
        var tenantContext = new FixedTenantContext(organizationId);
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;

        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);
        var run = ValidationRun.Record(organizationId, projectId, schemaVersionId, "deadbeef", [], executedByUserId);
        dbContext.ValidationRuns.Add(run);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SetTenantSessionVariableAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_tenant_id', @tenantId, false)";
        command.Parameters.AddWithValue("tenantId", tenantId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    private sealed class FixedTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? CurrentTenantId => tenantId;

        public void SetTenant(Guid organizationId) { }
    }
}
