using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record TeamMemberAdded(Guid TeamId, Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
