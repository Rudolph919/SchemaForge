using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence;

public sealed class SchemaForgeDbContext(
    DbContextOptions<SchemaForgeDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchemaForgeDbContext).Assembly);

        // The global query filter needs the runtime tenant context, so it's applied here rather
        // than inside OrganizationMembershipConfiguration (which is instantiated parameterlessly
        // by ApplyConfigurationsFromAssembly's reflection scan and has no way to receive it).
        // Guid == null-Guid? is structurally false for every row, so "no ambient tenant" fails
        // closed - it doesn't need an explicit null check to be safe (Step 5 §3).
        //
        // The matching RLS session variable (app.current_tenant_id) is set by
        // TenantSessionConnectionInterceptor on every connection open, not here - that covers
        // both reads and writes uniformly. An earlier version of this class set it manually
        // inside an overridden SaveChangesAsync, which only ever protected writes; nothing built
        // before the integration tests happened to exercise a tenant-scoped read, so that gap
        // went unnoticed until a test specifically exercised one.
        modelBuilder.Entity<OrganizationMembership>()
            .HasQueryFilter(m => m.OrganizationId == tenantContext.CurrentTenantId);
    }
}
