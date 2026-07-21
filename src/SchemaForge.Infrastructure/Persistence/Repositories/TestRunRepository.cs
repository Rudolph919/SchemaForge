using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Testing;
using SchemaForge.Domain.Testing;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class TestRunRepository(SchemaForgeDbContext dbContext) : ITestRunRepository
{
    public Task<TestRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.TestRuns.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddAsync(TestRun run, CancellationToken cancellationToken) =>
        await dbContext.TestRuns.AddAsync(run, cancellationToken);
}
