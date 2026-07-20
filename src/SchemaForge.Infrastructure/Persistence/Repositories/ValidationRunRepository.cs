using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Validation;
using SchemaForge.Domain.Validation;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class ValidationRunRepository(SchemaForgeDbContext dbContext) : IValidationRunRepository
{
    public async Task AddAsync(ValidationRun run, CancellationToken cancellationToken) =>
        await dbContext.ValidationRuns.AddAsync(run, cancellationToken);

    public async Task<IReadOnlyList<ValidationRun>> GetAllForSchemaVersionAsync(
        Guid schemaVersionId, CancellationToken cancellationToken) =>
        await dbContext.ValidationRuns
            .Where(r => r.SchemaVersionId == schemaVersionId)
            .OrderByDescending(r => r.ExecutedAt)
            .ToListAsync(cancellationToken);
}
