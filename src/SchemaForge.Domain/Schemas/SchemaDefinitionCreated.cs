using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas;

public sealed record SchemaDefinitionCreated(Guid OrganizationId, Guid ProjectId, Guid SchemaDefinitionId, string Name)
    : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
