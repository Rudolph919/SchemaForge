using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Application.Workspaces;

public interface ISourceDocumentRepository
{
    Task<SourceDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SourceDocument>> GetAllForProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task AddAsync(SourceDocument document, CancellationToken cancellationToken);

    void Remove(SourceDocument document);
}
