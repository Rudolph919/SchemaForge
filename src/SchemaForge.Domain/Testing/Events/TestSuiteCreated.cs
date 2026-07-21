using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing.Events;

public sealed record TestSuiteCreated(Guid OrganizationId, Guid SchemaDefinitionId, Guid TestSuiteId, string Name)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "TestSuite.Created";
    public string EntityType => "TestSuite";
    public Guid EntityId => TestSuiteId;
    public string? MetadataJson => JsonSerializer.Serialize(new { Name, SchemaDefinitionId });
}
