using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas.Events;

public sealed record SchemaNodeRemoved(Guid SchemaVersionId, Guid NodeId) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "SchemaVersion.NodeRemoved";
    public string EntityType => "SchemaVersion";
    public Guid EntityId => SchemaVersionId;
    public string? MetadataJson => JsonSerializer.Serialize(new { NodeId });
}
