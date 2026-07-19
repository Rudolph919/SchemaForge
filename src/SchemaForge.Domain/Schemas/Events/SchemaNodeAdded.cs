using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas.Events;

public sealed record SchemaNodeAdded(Guid SchemaVersionId, Guid NodeId, string? PropertyName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
