using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record OrganizationMembershipInvited(Guid OrganizationId, Guid UserId, OrganizationRole Role)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "OrganizationMembership.Invited";
    public string EntityType => "OrganizationMembership";
    public Guid EntityId => UserId;
    public string? MetadataJson => JsonSerializer.Serialize(new { Role = Role.ToString() });
}
