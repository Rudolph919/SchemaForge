using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record TeamCreated(Guid OrganizationId, Guid TeamId, string Name) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
