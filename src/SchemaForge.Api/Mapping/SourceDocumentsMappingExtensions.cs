using SchemaForge.Application.Workspaces.Commands.UploadSourceDocument;
using SchemaForge.Application.Workspaces.Queries.ListSourceDocuments;
using SchemaForge.Contracts.V1.SourceDocuments;

namespace SchemaForge.Api.Mapping;

public static class SourceDocumentsMappingExtensions
{
    public static UploadSourceDocumentResponse ToResponse(this UploadSourceDocumentResult result) =>
        new(result.DocumentId);

    public static SourceDocumentResponse ToResponse(this SourceDocumentSummary summary) =>
        new(summary.Id, summary.FileName, summary.ContentType, summary.SizeBytes, summary.CreatedAt);
}
