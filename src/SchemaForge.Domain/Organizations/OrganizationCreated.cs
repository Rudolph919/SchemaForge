using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record OrganizationCreated(Guid OrganizationId, string Name) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
