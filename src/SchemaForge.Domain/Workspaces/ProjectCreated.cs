using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Workspaces;

public sealed record ProjectCreated(Guid OrganizationId, Guid ProjectId, string Name) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "Project.Created";
    public string EntityType => "Project";
    public Guid EntityId => ProjectId;
    public string? MetadataJson => JsonSerializer.Serialize(new { Name });
}
