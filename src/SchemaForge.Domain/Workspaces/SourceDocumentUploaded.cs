using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Workspaces;

public sealed record SourceDocumentUploaded(Guid OrganizationId, Guid ProjectId, Guid DocumentId, string FileName)
    : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "SourceDocument.Uploaded";
    public string EntityType => "SourceDocument";
    public Guid EntityId => DocumentId;
    public string? MetadataJson => JsonSerializer.Serialize(new { FileName, ProjectId });
}
