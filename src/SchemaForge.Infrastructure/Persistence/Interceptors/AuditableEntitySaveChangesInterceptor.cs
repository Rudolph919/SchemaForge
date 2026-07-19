using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchemaForge.SharedKernel;

namespace SchemaForge.Infrastructure.Persistence.Interceptors;

// Stamps CreatedAt/UpdatedAt automatically. Actor stamping (CreatedByUserId/UpdatedByUserId) is
// deliberately NOT automatic here - registration and org creation happen before any JWT exists,
// so there's no ambient "current user" to attribute them to at this point in the vertical slice.
// Revisit once there's an authenticated write path that legitimately has one.
public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void StampTimestamps(DbContext? context)
    {
        if (context is null) return;

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditableTimestamps) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditableTimestamps.CreatedAt)).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(IAuditableTimestamps.UpdatedAt)).CurrentValue = now;
                    break;
            }
        }
    }
}
