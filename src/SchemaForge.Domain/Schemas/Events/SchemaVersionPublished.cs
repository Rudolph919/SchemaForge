using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas.Events;

public sealed record SchemaVersionPublished(Guid SchemaDefinitionId, Guid SchemaVersionId) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "SchemaVersion.Published";
    public string EntityType => "SchemaVersion";
    public Guid EntityId => SchemaVersionId;
    public string? MetadataJson => null;
}
