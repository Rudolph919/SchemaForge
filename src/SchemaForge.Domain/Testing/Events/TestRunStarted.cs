using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Testing.Events;

public sealed record TestRunStarted(Guid OrganizationId, Guid TestSuiteId, Guid SchemaVersionId, Guid TestRunId)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "TestRun.Started";
    public string EntityType => "TestRun";
    public Guid EntityId => TestRunId;
    public string? MetadataJson => JsonSerializer.Serialize(new { TestSuiteId, SchemaVersionId });
}
