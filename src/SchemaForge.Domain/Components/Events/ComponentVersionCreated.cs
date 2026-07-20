using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Components.Events;

public sealed record ComponentVersionCreated(Guid ComponentDefinitionId, Guid ComponentVersionId, SemVer VersionNumber)
    : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
