namespace SchemaForge.SharedKernel;

// Linearizes Step 4's illustrative "TenantOwnedAggregateRoot : AggregateRoot, AuditableEntity" sketch
// into a single hierarchy, since C# has no multiple class inheritance. CreatedByUserId is nullable
// to accommodate self-registration/system-created rows (e.g. a User created by nobody else).
public abstract class AuditableEntity<TId> : AggregateRoot<TId>
    where TId : notnull
{
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    protected AuditableEntity(TId id) : base(id) { }

    protected AuditableEntity() { }
}
