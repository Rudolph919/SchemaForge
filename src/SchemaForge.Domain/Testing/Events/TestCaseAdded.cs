using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing.Events;

public sealed record TestCaseAdded(Guid TestSuiteId, Guid TestCaseId, string Name) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
