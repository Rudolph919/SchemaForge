using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas.Events;

public sealed record SchemaVersionPublished(Guid SchemaDefinitionId, Guid SchemaVersionId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
