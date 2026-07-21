using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas;

public sealed record SchemaDefinitionCreated(Guid OrganizationId, Guid ProjectId, Guid SchemaDefinitionId, string Name)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "SchemaDefinition.Created";
    public string EntityType => "SchemaDefinition";
    public Guid EntityId => SchemaDefinitionId;
    public string? MetadataJson => JsonSerializer.Serialize(new { Name, ProjectId });
}
