using Testcontainers.Redis;

namespace SchemaForge.IntegrationTests.Fixtures;

// Same pattern as MinioFixture/PostgresFixture: one container shared across the whole test run
// via ICollectionFixture. Needed now that both IDocumentationCache and the Idempotency-Key
// middleware genuinely depend on IDistributedCache resolving to a real Redis connection - without
// this, GetConnectionString("Redis") is null in the Testing environment and StackExchange.Redis
// throws ArgumentNullException the first time anything actually calls into it (confirmed live:
// every idempotent POST 500'd until this fixture existed).
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Same mechanism as PostgresFixture/MinioFixture - environment variable, read by
        // WebApplication.CreateBuilder before any application code runs.
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", _container.GetConnectionString());
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
