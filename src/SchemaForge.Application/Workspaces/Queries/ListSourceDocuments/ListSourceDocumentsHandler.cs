using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Queries.ListSourceDocuments;

public sealed class ListSourceDocumentsHandler(
    IProjectRepository projectRepository, ISourceDocumentRepository sourceDocumentRepository)
    : IRequestHandler<ListSourceDocumentsQuery, Result<IReadOnlyList<SourceDocumentSummary>>>
{
    public async Task<Result<IReadOnlyList<SourceDocumentSummary>>> Handle(
        ListSourceDocumentsQuery request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<IReadOnlyList<SourceDocumentSummary>>.Failure(
                Error.NotFound("Project.NotFound", "No such project."));
        }

        var documents = await sourceDocumentRepository.GetAllForProjectAsync(request.ProjectId, cancellationToken);

        var summaries = documents
            .Select(d => new SourceDocumentSummary(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<SourceDocumentSummary>>.Success(summaries);
    }
}
