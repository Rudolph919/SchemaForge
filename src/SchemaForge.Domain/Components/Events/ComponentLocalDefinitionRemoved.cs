using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Components.Events;

public sealed record ComponentLocalDefinitionRemoved(Guid ComponentVersionId, Guid LocalDefinitionId) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "ComponentVersion.LocalDefinitionRemoved";
    public string EntityType => "ComponentVersion";
    public Guid EntityId => ComponentVersionId;
    public string? MetadataJson => JsonSerializer.Serialize(new { LocalDefinitionId });
}
