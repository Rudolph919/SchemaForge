using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Workspaces;

// Immutable once uploaded (Step 3 §3) - re-uploading the "same" document is a new SourceDocument,
// never an in-place replace, so the record of what was actually referenced when never changes
// out from under anything that pointed at it. No mutation methods beyond Create as a result.
public sealed class SourceDocument : TenantOwnedAggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }

    public string FileName { get; private set; } = null!;

    public string StorageKey { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    private SourceDocument() { } // EF Core materialization

    private SourceDocument(
        Guid id,
        Guid organizationId,
        Guid projectId,
        string fileName,
        string storageKey,
        string contentType,
        long sizeBytes)
        : base(id, organizationId)
    {
        ProjectId = projectId;
        FileName = fileName;
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
    }

    public static SourceDocument Create(
        Guid organizationId,
        Guid projectId,
        string fileName,
        string storageKey,
        string contentType,
        long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentException("Size must be positive.", nameof(sizeBytes));
        }

        var document = new SourceDocument(
            Guid.NewGuid(), organizationId, projectId, fileName, storageKey, contentType, sizeBytes);
        document.RaiseDomainEvent(
            new SourceDocumentUploaded(organizationId, projectId, document.Id, fileName));

        return document;
    }
}
