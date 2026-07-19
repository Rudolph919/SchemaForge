using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class SchemaDefinitionRepository(SchemaForgeDbContext dbContext) : ISchemaDefinitionRepository
{
    public Task<SchemaDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SchemaDefinitions.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SchemaDefinition>> GetAllForProjectAsync(
        Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.SchemaDefinitions.Where(d => d.ProjectId == projectId).ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(Guid projectId, string name, CancellationToken cancellationToken) =>
        dbContext.SchemaDefinitions.AnyAsync(d => d.ProjectId == projectId && d.Name == name, cancellationToken);

    public async Task AddAsync(SchemaDefinition definition, CancellationToken cancellationToken) =>
        await dbContext.SchemaDefinitions.AddAsync(definition, cancellationToken);
}
