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
        modelBuilder.Entity<OrganizationMembership>()
            .HasQueryFilter(m => m.OrganizationId == tenantContext.CurrentTenantId);
    }

    // Sets the RLS session variable inside the SAME transaction the actual writes use - this is
    // the whole mechanism RLS depends on, so it can't be left to a SaveChangesInterceptor. A
    // SavingChangesAsync interceptor callback fires BEFORE EF Core begins its own ambient
    // transaction for the save; raw SQL issued there would run as its own separate implicit
    // transaction and vanish (SET LOCAL is transaction-scoped) before the actual INSERT/UPDATE
    // ever ran. Explicitly opening the transaction first, running set_config inside it, then
    // calling SaveChanges (which detects and reuses the already-open transaction rather than
    // starting its own) is the reliable way to guarantee both statements share one transaction.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (tenantContext.CurrentTenantId is not { } tenantId)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

        await Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_tenant_id', {tenantId.ToString()}, true)",
            cancellationToken);

        var result = await base.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}
