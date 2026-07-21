using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Components.Events;

public sealed record ComponentNodeAdded(Guid ComponentVersionId, Guid NodeId, string? PropertyName)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "ComponentVersion.NodeAdded";
    public string EntityType => "ComponentVersion";
    public Guid EntityId => ComponentVersionId;
    public string? MetadataJson => JsonSerializer.Serialize(new { NodeId, PropertyName });
}
