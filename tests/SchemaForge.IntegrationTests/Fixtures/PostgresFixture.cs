using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SchemaForge.IntegrationTests.Fixtures;

// Shared across every integration test class via ICollectionFixture (one container per test
// run, not per class) - spinning up a fresh Postgres container per test class would make the
// suite unusably slow.
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string AppRoleUsername = "schemaforge_app_test";
    private const string AppRolePassword = "test-app-password";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("schemaforge")
        .WithUsername("schemaforge")
        .WithPassword("test-superuser-password")
        .Build();

    // The migration/DDL role - tests must never run application code against this connection,
    // only migrations and direct setup/assertion SQL. Using it for app traffic would silently
    // make every RLS test pass for the wrong reason (superusers always bypass RLS - the exact
    // bug the Infrastructure PR found and fixed in the real docker-compose setup).
    public string SuperuserConnectionString => _container.GetConnectionString();

    // What the app under test actually connects as, matching docker-compose's schemaforge_app
    // role exactly.
    public string AppConnectionString
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Username = AppRoleUsername,
                Password = AppRolePassword
            };
            return builder.ConnectionString;
        }
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Mirrors docker/postgres-init/01-create-app-role.sh.
        var scriptResult = await _container.ExecScriptAsync($"""
            CREATE ROLE {AppRoleUsername} WITH LOGIN PASSWORD '{AppRolePassword}' NOSUPERUSER NOBYPASSRLS;
            GRANT CONNECT ON DATABASE schemaforge TO {AppRoleUsername};
            GRANT USAGE ON SCHEMA public TO {AppRoleUsername};
            ALTER DEFAULT PRIVILEGES FOR ROLE schemaforge IN SCHEMA public
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {AppRoleUsername};
            ALTER DEFAULT PRIVILEGES FOR ROLE schemaforge IN SCHEMA public
                GRANT USAGE, SELECT ON SEQUENCES TO {AppRoleUsername};
            """);

        if (scriptResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to create the restricted test app role: {scriptResult.Stderr}");
        }

        await using var dbContext = CreateMigrationContext();
        await dbContext.Database.MigrateAsync();

        // Environment variables, not WebApplicationFactory.ConfigureAppConfiguration: they're
        // read by WebApplication.CreateBuilder itself, at the very start of Program.cs, before
        // any application code runs - the same mechanism docker-compose already uses in
        // production (ConnectionStrings__Default etc.), so there's no timing question about
        // whether an override "wins" against the app's own configuration reads. Safe to set
        // process-wide here specifically because every test class in this run shares this one
        // fixture and therefore this one connection string.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", AppConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-signing-key-minimum-32-characters-long");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "SchemaForge");
        Environment.SetEnvironmentVariable("Jwt__Audience", "SchemaForge");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Cors__FrontendOrigin", "http://localhost:5173");
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private SchemaForgeDbContext CreateMigrationContext()
    {
        var options = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(SuperuserConnectionString)
            .Options;

        return new SchemaForgeDbContext(options, new NoOpTenantContext());
    }

    private sealed class NoOpTenantContext : ITenantContext
    {
        public Guid? CurrentTenantId => null;

        public void SetTenant(Guid organizationId) { }
    }
}
