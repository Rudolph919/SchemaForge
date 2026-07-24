using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Components.Events;

public sealed record ComponentLocalDefinitionAdded(Guid ComponentVersionId, Guid LocalDefinitionId, string Name)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "ComponentVersion.LocalDefinitionAdded";
    public string EntityType => "ComponentVersion";
    public Guid EntityId => ComponentVersionId;
    public string? MetadataJson => JsonSerializer.Serialize(new { LocalDefinitionId, Name });
}
