namespace SchemaForge.SharedKernel;

// Base for every tenant-scoped aggregate root. OrganizationId is what the EF Core global query
// filter and the Postgres RLS policy both key off (Step 5 §3) — every table backing one of these
// carries this column.
public abstract class TenantOwnedAggregateRoot<TId> : AuditableEntity<TId>
    where TId : notnull
{
    public Guid OrganizationId { get; protected set; }

    protected TenantOwnedAggregateRoot(TId id, Guid organizationId) : base(id) =>
        OrganizationId = organizationId;

    protected TenantOwnedAggregateRoot() { }
}
