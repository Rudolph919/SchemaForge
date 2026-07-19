using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.Persistence;

// Only used by `dotnet ef migrations` tooling at design time - the real app always resolves
// SchemaForgeDbContext (and the real ITenantContext) through DI, wired in Api's
// InfrastructureServiceCollectionExtensions.
public sealed class SchemaForgeDbContextFactory : IDesignTimeDbContextFactory<SchemaForgeDbContext>
{
    public SchemaForgeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SCHEMAFORGE_DB_CONNECTION")
            ?? "Host=localhost;Database=schemaforge;Username=schemaforge;Password=changeme-local-only";

        var optionsBuilder = new DbContextOptionsBuilder<SchemaForgeDbContext>()
            .UseNpgsql(connectionString);

        return new SchemaForgeDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? CurrentTenantId => null;

        public void SetTenant(Guid organizationId) { }
    }
}
