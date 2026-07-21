using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing.Events;

public sealed record TestCaseUpdated(Guid TestSuiteId, Guid TestCaseId) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "TestSuite.CaseUpdated";
    public string EntityType => "TestSuite";
    public Guid EntityId => TestSuiteId;
    public string? MetadataJson => JsonSerializer.Serialize(new { TestCaseId });
}
