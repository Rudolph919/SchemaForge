using SchemaForge.Application.Common.Abstractions;

namespace SchemaForge.Infrastructure.Persistence;

public sealed class UnitOfWork(SchemaForgeDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
