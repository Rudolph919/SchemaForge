using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record OrganizationMembershipRoleChanged(
    Guid OrganizationId, Guid UserId, OrganizationRole OldRole, OrganizationRole NewRole) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
