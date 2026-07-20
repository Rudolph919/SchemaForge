using SchemaForge.Domain.Testing;

namespace SchemaForge.Application.Testing;

public interface ITestSuiteRepository
{
    Task<TestSuite?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TestSuiteSummary>> GetAllForSchemaDefinitionAsync(
        Guid schemaDefinitionId, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(Guid schemaDefinitionId, string name, CancellationToken cancellationToken);

    Task AddAsync(TestSuite suite, CancellationToken cancellationToken);
}

public sealed record TestSuiteSummary(Guid Id, string Name, string? Description, int CaseCount);
