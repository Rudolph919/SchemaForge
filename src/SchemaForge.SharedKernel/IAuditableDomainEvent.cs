namespace SchemaForge.SharedKernel;

// Optional per-event contract for domain events that should produce an AuditLogEntry (Step 8
// §4's Open Host Service / Published Language). Audit Log's projector checks for this via a type
// pattern match against the base IDomainEvent it already receives - a brand new event in a brand
// new bounded context is audited automatically the moment it implements this interface, with
// zero code change in Audit Log itself, which is the whole point of the pattern.
public interface IAuditableDomainEvent : IDomainEvent
{
    string Action { get; }

    string EntityType { get; }

    Guid EntityId { get; }

    string? MetadataJson { get; }
}
