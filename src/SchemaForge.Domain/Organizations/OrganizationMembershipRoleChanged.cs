using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record OrganizationMembershipRoleChanged(
    Guid OrganizationId, Guid UserId, OrganizationRole OldRole, OrganizationRole NewRole) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "OrganizationMembership.RoleChanged";
    public string EntityType => "OrganizationMembership";
    public Guid EntityId => UserId;
    public string? MetadataJson =>
        JsonSerializer.Serialize(new { OldRole = OldRole.ToString(), NewRole = NewRole.ToString() });
}
