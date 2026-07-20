using SchemaForge.Domain.Components;

namespace SchemaForge.Application.Components;

public interface IComponentDefinitionRepository
{
    Task<ComponentDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ComponentDefinition>> GetAllForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken);

    Task AddAsync(ComponentDefinition definition, CancellationToken cancellationToken);
}
