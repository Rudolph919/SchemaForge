using Microsoft.EntityFrameworkCore;
using SchemaForge.SharedKernel;

namespace SchemaForge.Infrastructure.Persistence;

// Shared by every repository's ApplyExpectedVersion method (Step 6 §1.5) - overwrites the
// tracked entity's OriginalValue for its xmin-backed RowVersion with the client's claimed
// If-Match value, so EF Core's own concurrency check (comparing OriginalValue against the row's
// actual current xmin at UPDATE time) compares against what the client last saw, not just
// whatever this same request happened to read moments ago.
public static class ConcurrencyTokenExtensions
{
    public static void ApplyExpectedVersion<TEntity>(this DbContext context, TEntity entity, uint expectedVersion)
        where TEntity : class, IHasRowVersion =>
        context.Entry(entity).Property(nameof(IHasRowVersion.RowVersion)).OriginalValue = expectedVersion;
}
