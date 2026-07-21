using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Components.Events;

public sealed record ComponentDefinitionCreated(Guid OrganizationId, Guid ComponentDefinitionId, string Name)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "ComponentDefinition.Created";
    public string EntityType => "ComponentDefinition";
    public Guid EntityId => ComponentDefinitionId;
    public string? MetadataJson => JsonSerializer.Serialize(new { Name });
}
