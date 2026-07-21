using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing.Events;

public sealed record TestCaseAdded(Guid TestSuiteId, Guid TestCaseId, string Name) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "TestSuite.CaseAdded";
    public string EntityType => "TestSuite";
    public Guid EntityId => TestSuiteId;
    public string? MetadataJson => JsonSerializer.Serialize(new { TestCaseId, Name });
}
