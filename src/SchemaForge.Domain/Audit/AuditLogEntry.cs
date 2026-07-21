using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Audit;

// Immutable, append-only (Step 3 §3) - a record of a past event, not something anyone edits.
// Populated exclusively by AuditLogEntryProjector reacting to other aggregates' domain events
// (Step 8 §4's Open Host Service) - nothing else should ever construct one.
public sealed class AuditLogEntry : TenantOwnedAggregateRoot<Guid>
{
    public Guid ActorUserId { get; private set; }

    public string Action { get; private set; } = null!;

    public string EntityType { get; private set; } = null!;

    public Guid EntityId { get; private set; }

    public string? MetadataJson { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    private AuditLogEntry() { } // EF Core materialization

    private AuditLogEntry(
        Guid id, Guid organizationId, Guid actorUserId, string action, string entityType, Guid entityId,
        string? metadataJson, DateTimeOffset occurredAt)
        : base(id, organizationId)
    {
        ActorUserId = actorUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        MetadataJson = metadataJson;
        OccurredAt = occurredAt;
    }

    public static AuditLogEntry Record(
        Guid organizationId, Guid actorUserId, string action, string entityType, Guid entityId,
        string? metadataJson, DateTimeOffset occurredAt) =>
        new(Guid.NewGuid(), organizationId, actorUserId, action, entityType, entityId, metadataJson, occurredAt);
}
