using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Infrastructure.Persistence;
using SchemaForge.Infrastructure.Persistence.Interceptors;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.MultiTenancy;

// Asserts the EF Core global query filter layer specifically (Step 5 §3's first isolation
// layer) - RowLevelSecurityTests asserts the second (Postgres RLS) independently.
[Collection(nameof(IntegrationTestCollection))]
public sealed class QueryFilterIsolationTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task A_tenant_scoped_DbContext_never_sees_another_organizations_membership()
    {
        var orgA = await RegisterAsync("Org A");
        var orgB = await RegisterAsync("Org B");

        var membershipsVisibleToOrgA = await QueryMembershipsAsync(orgA.OrganizationId);

        membershipsVisibleToOrgA.Should().ContainSingle(m => m == orgA.OrganizationId);
        membershipsVisibleToOrgA.Should().NotContain(orgB.OrganizationId);
    }

    [Fact]
    public async Task A_DbContext_with_no_tenant_context_sees_nothing()
    {
        await RegisterAsync("Org With A Member");

        var membershipsVisibleWithNoTenant = await QueryMembershipsAsync(tenantId: null);

        membershipsVisibleWithNoTenant.Should().BeEmpty();
    }

    private async Task<RegisterResponse> RegisterAsync(string organizationName)
    {
        var request = new RegisterRequest(
            $"{Guid.NewGuid()}@example.com", "correct-horse-battery", "Test User", organizationName);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    private async Task<List<Guid>> QueryMembershipsAsync(Guid? tenantId)
    {
        var tenantContext = new FixedTenantContext(tenantId);

        // The RLS session variable is only ever set by TenantSessionConnectionInterceptor, which
        // the real app attaches via AddInfrastructure's AddDbContext call - a manually
        // constructed DbContext needs the same interceptor explicitly, or every query is blocked
        // by RLS (even a tenant's own data), not just cross-tenant ones.
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(postgres.AppConnectionString)
            .AddInterceptors(new TenantSessionConnectionInterceptor(tenantContext))
            .Options;

        await using var dbContext = new SchemaForgeDbContext(options, tenantContext);

        return await dbContext.OrganizationMemberships.Select(m => m.OrganizationId).ToListAsync();
    }

    private sealed class FixedTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? CurrentTenantId => tenantId;

        public void SetTenant(Guid organizationId) { }
    }
}
