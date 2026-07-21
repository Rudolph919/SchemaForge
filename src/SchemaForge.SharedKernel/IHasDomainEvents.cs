namespace SchemaForge.SharedKernel;

// Non-generic so Infrastructure's SaveChangesInterceptor can find any aggregate with pending
// domain events via a single EF Core `ChangeTracker.Entries<IHasDomainEvents>()` call, regardless
// of its TId - the same reasoning as IAuditableTimestamps existing separately from
// AuditableEntity<TId>.
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
