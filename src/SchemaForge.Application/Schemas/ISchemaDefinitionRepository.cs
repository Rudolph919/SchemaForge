using SchemaForge.Domain.Schemas;

namespace SchemaForge.Application.Schemas;

public interface ISchemaDefinitionRepository
{
    Task<SchemaDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SchemaDefinition>> GetAllForProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(Guid projectId, string name, CancellationToken cancellationToken);

    Task AddAsync(SchemaDefinition definition, CancellationToken cancellationToken);

    void ApplyExpectedVersion(SchemaDefinition definition, uint expectedVersion);
}
