using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Audit;
using SchemaForge.Domain.Components;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Testing;
using SchemaForge.Domain.Validation;
using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Infrastructure.Persistence;

public sealed class SchemaForgeDbContext(
    DbContextOptions<SchemaForgeDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<SourceDocument> SourceDocuments => Set<SourceDocument>();

    public DbSet<SchemaDefinition> SchemaDefinitions => Set<SchemaDefinition>();

    public DbSet<SchemaVersion> SchemaVersions => Set<SchemaVersion>();

    public DbSet<ValidationRun> ValidationRuns => Set<ValidationRun>();

    public DbSet<ComponentDefinition> ComponentDefinitions => Set<ComponentDefinition>();

    public DbSet<ComponentVersion> ComponentVersions => Set<ComponentVersion>();

    public DbSet<TestSuite> TestSuites => Set<TestSuite>();

    public DbSet<TestRun> TestRuns => Set<TestRun>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchemaForgeDbContext).Assembly);

        // The global query filter needs the runtime tenant context, so it's applied here rather
        // than inside each entity's IEntityTypeConfiguration (which is instantiated
        // parameterlessly by ApplyConfigurationsFromAssembly's reflection scan and has no way to
        // receive it). Guid == null-Guid? is structurally false for every row, so "no ambient
        // tenant" fails closed - it doesn't need an explicit null check to be safe (Step 5 §3).
        //
        // The matching RLS session variable (app.current_tenant_id) is set by
        // TenantSessionConnectionInterceptor on every connection open, covering reads and writes
        // uniformly (Step 0's integration-tests PR found and fixed the read-path gap).
        modelBuilder.Entity<OrganizationMembership>()
            .HasQueryFilter(m => m.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<Team>()
            .HasQueryFilter(t => t.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<Project>()
            .HasQueryFilter(p => p.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<SourceDocument>()
            .HasQueryFilter(d => d.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<SchemaDefinition>()
            .HasQueryFilter(d => d.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<SchemaVersion>()
            .HasQueryFilter(v => v.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<ValidationRun>()
            .HasQueryFilter(r => r.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<ComponentDefinition>()
            .HasQueryFilter(d => d.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<ComponentVersion>()
            .HasQueryFilter(v => v.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<TestSuite>()
            .HasQueryFilter(s => s.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<TestRun>()
            .HasQueryFilter(r => r.OrganizationId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<AuditLogEntry>()
            .HasQueryFilter(e => e.OrganizationId == tenantContext.CurrentTenantId);
    }
}
