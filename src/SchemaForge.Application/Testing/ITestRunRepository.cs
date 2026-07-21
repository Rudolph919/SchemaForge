using SchemaForge.Domain.Testing;

namespace SchemaForge.Application.Testing;

public interface ITestRunRepository
{
    Task<TestRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(TestRun run, CancellationToken cancellationToken);
}
