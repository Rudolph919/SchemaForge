using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Components;
using SchemaForge.Domain.Components;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class ComponentDefinitionRepository(SchemaForgeDbContext dbContext) : IComponentDefinitionRepository
{
    public Task<ComponentDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.ComponentDefinitions.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ComponentDefinition>> GetAllForOrganizationAsync(
        Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.ComponentDefinitions.Where(d => d.OrganizationId == organizationId).ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken) =>
        dbContext.ComponentDefinitions.AnyAsync(d => d.OrganizationId == organizationId && d.Name == name, cancellationToken);

    public async Task AddAsync(ComponentDefinition definition, CancellationToken cancellationToken) =>
        await dbContext.ComponentDefinitions.AddAsync(definition, cancellationToken);
}
