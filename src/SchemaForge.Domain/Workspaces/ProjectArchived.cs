using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Workspaces;

public sealed record ProjectArchived(Guid ProjectId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
