using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Identity;

public sealed record UserRegistered(Guid UserId, string Email) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
