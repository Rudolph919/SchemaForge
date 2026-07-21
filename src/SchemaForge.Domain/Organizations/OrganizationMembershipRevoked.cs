using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record OrganizationMembershipRevoked(Guid OrganizationId, Guid UserId) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "OrganizationMembership.Revoked";
    public string EntityType => "OrganizationMembership";
    public Guid EntityId => UserId;
    public string? MetadataJson => null;
}
