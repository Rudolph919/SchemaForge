using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

// Same "not auditable in practice yet" note as UserRegistered - raised during the same pre-auth
// registration flow, before any ambient tenant/actor exists for AuditLogEntryProjector to use.
public sealed record OrganizationCreated(Guid OrganizationId, string Name) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "Organization.Created";
    public string EntityType => "Organization";
    public Guid EntityId => OrganizationId;
    public string? MetadataJson => JsonSerializer.Serialize(new { Name });
}
