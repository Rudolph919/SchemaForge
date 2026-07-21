using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Workspaces;

public sealed record ProjectReactivated(Guid ProjectId) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "Project.Reactivated";
    public string EntityType => "Project";
    public Guid EntityId => ProjectId;
    public string? MetadataJson => null;
}
