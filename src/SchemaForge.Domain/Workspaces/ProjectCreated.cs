using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Workspaces;

public sealed record ProjectCreated(Guid OrganizationId, Guid ProjectId, string Name) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
