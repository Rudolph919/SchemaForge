using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Queries.ListSourceDocuments;

public sealed record ListSourceDocumentsQuery(Guid ProjectId) : IQuery<Result<IReadOnlyList<SourceDocumentSummary>>>;

public sealed record SourceDocumentSummary(
    Guid Id, string FileName, string ContentType, long SizeBytes, DateTimeOffset CreatedAt);
