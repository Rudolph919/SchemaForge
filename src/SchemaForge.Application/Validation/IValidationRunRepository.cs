using SchemaForge.Domain.Validation;

namespace SchemaForge.Application.Validation;

public interface IValidationRunRepository
{
    Task AddAsync(ValidationRun run, CancellationToken cancellationToken);

    Task<IReadOnlyList<ValidationRun>> GetAllForSchemaVersionAsync(Guid schemaVersionId, CancellationToken cancellationToken);
}
