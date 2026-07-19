using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Workspaces;

public sealed record SourceDocumentUploaded(Guid OrganizationId, Guid ProjectId, Guid DocumentId, string FileName)
    : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
