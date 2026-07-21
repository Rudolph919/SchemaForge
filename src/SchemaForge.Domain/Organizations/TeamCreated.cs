using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record TeamCreated(Guid OrganizationId, Guid TeamId, string Name) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "Team.Created";
    public string EntityType => "Team";
    public Guid EntityId => TeamId;
    public string? MetadataJson => JsonSerializer.Serialize(new { Name });
}
