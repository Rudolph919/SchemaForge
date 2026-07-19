using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record OrganizationMembershipRevoked(Guid OrganizationId, Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
