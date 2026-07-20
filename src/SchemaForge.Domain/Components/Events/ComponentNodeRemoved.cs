using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Components.Events;

public sealed record ComponentNodeRemoved(Guid ComponentVersionId, Guid NodeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
