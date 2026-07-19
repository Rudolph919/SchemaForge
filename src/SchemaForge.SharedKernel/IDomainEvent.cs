namespace SchemaForge.SharedKernel;

// Dispatched by Infrastructure's SaveChangesInterceptor only after the triggering transaction commits.
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
