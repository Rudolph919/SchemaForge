using System.Text.Json;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Schemas.Events;

public sealed record SchemaVersionCreated(Guid SchemaDefinitionId, Guid SchemaVersionId, SemVer VersionNumber)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "SchemaVersion.Created";
    public string EntityType => "SchemaVersion";
    public Guid EntityId => SchemaVersionId;
    public string? MetadataJson => JsonSerializer.Serialize(new { VersionNumber = VersionNumber.ToString() });
}
