using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Workspaces;
using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class SourceDocumentRepository(SchemaForgeDbContext dbContext) : ISourceDocumentRepository
{
    public Task<SourceDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SourceDocuments.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SourceDocument>> GetAllForProjectAsync(
        Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.SourceDocuments.Where(d => d.ProjectId == projectId).ToListAsync(cancellationToken);

    public async Task AddAsync(SourceDocument document, CancellationToken cancellationToken) =>
        await dbContext.SourceDocuments.AddAsync(document, cancellationToken);

    public void Remove(SourceDocument document) => dbContext.SourceDocuments.Remove(document);
}
