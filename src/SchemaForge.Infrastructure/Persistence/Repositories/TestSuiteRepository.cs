using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Testing;
using SchemaForge.Domain.Testing;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class TestSuiteRepository(SchemaForgeDbContext dbContext) : ITestSuiteRepository
{
    // Cases is an EF Core owned collection, loaded automatically with its owner - no explicit
    // .Include() needed (same as Team.Members).
    public Task<TestSuite?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.TestSuites.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TestSuiteSummary>> GetAllForSchemaDefinitionAsync(
        Guid schemaDefinitionId, CancellationToken cancellationToken) =>
        await dbContext.TestSuites
            .Where(s => s.SchemaDefinitionId == schemaDefinitionId)
            .Select(s => new TestSuiteSummary(s.Id, s.Name, s.Description, s.Cases.Count))
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(Guid schemaDefinitionId, string name, CancellationToken cancellationToken) =>
        dbContext.TestSuites.AnyAsync(s => s.SchemaDefinitionId == schemaDefinitionId && s.Name == name, cancellationToken);

    public async Task AddAsync(TestSuite suite, CancellationToken cancellationToken) =>
        await dbContext.TestSuites.AddAsync(suite, cancellationToken);
}
