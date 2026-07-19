namespace SchemaForge.SharedKernel;

// Non-generic so Infrastructure's SaveChangesInterceptor can find any auditable entity via a
// single type check, regardless of its TId - a generic AuditableEntity<TId> alone would force
// the interceptor to know every concrete TId in use.
public interface IAuditableTimestamps
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
}

// Linearizes Step 4's illustrative "TenantOwnedAggregateRoot : AggregateRoot, AuditableEntity" sketch
// into a single hierarchy, since C# has no multiple class inheritance. CreatedByUserId is nullable
// to accommodate self-registration/system-created rows (e.g. a User created by nobody else).
public abstract class AuditableEntity<TId> : AggregateRoot<TId>, IAuditableTimestamps
    where TId : notnull
{
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    protected AuditableEntity(TId id) : base(id) { }

    protected AuditableEntity() { }
}
