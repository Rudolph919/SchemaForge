using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Components.Events;

public sealed record ComponentNodeAdded(Guid ComponentVersionId, Guid NodeId, string? PropertyName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
