namespace SchemaForge.Contracts.V1.SourceDocuments;

public sealed record SourceDocumentResponse(
    Guid Id, string FileName, string ContentType, long SizeBytes, DateTimeOffset CreatedAt);
