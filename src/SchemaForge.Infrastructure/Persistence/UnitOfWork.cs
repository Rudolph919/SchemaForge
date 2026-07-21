using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Common.Exceptions;

namespace SchemaForge.Infrastructure.Persistence;

public sealed class UnitOfWork(SchemaForgeDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Translated to a dependency-free exception here - Application (where
            // TransactionBehavior actually catches this) must not reference EF Core.
            throw new ConcurrencyConflictException();
        }
    }
}
