using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Components.Events;

public sealed record ComponentVersionDeprecated(Guid ComponentDefinitionId, Guid ComponentVersionId)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "ComponentVersion.Deprecated";
    public string EntityType => "ComponentVersion";
    public Guid EntityId => ComponentVersionId;
    public string? MetadataJson => null;
}
