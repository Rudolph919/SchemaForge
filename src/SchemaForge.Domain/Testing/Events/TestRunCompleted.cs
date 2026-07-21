using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing.Events;

public sealed record TestRunCompleted(Guid TestRunId, Guid TestSuiteId, int CaseCount, int PassedCount)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "TestRun.Completed";
    public string EntityType => "TestRun";
    public Guid EntityId => TestRunId;
    public string? MetadataJson => JsonSerializer.Serialize(new { TestSuiteId, CaseCount, PassedCount });
}
