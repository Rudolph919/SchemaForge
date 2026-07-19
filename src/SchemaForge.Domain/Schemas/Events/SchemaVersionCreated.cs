using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Schemas.Events;

public sealed record SchemaVersionCreated(Guid SchemaDefinitionId, Guid SchemaVersionId, SemVer VersionNumber)
    : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
