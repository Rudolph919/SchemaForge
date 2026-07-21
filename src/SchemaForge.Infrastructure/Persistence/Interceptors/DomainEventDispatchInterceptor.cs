using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchemaForge.Application.Audit;
using SchemaForge.SharedKernel;

namespace SchemaForge.Infrastructure.Persistence.Interceptors;

// Collects domain events from every tracked aggregate and turns any IAuditableDomainEvent ones
// into AuditLogEntry rows, added to the SAME DbContext instance so they ride along in the SAME
// SaveChangesAsync call already in progress - not a separate post-commit save.
//
// This hooks SavingChangesAsync (before commit), not SavedChangesAsync (after) as Step 1 §7's
// illustrative design describes. Two failed attempts got here:
// 1. Post-commit + MediatR notification + the subscriber doing its own independent
//    SaveChangesAsync on the same DbContext instance - that nested SaveChangesAsync call
//    corrupted Npgsql's connection/reader state (confirmed live: the integration suite went from
//    49 passing to 24 failing with FK-constraint and empty-result errors).
// 2. Same pre-commit approach as now, but resolving IAuditLogEntryProjector via
//    context.GetInfrastructure() - that returns EF Core's own INTERNAL service container (for
//    EF-specific services like value converters), not the app's DI container, so the app-level
//    Scoped service was never actually found there. The resulting exception mid-SaveChanges
//    corrupted a pooled connection for whichever unrelated test reused it next (confirmed live:
//    32 failures, cascading across unrelated tests, not just ones that raise audited events).
//
// Constructor-injecting IAuditLogEntryProjector and registering this interceptor Scoped (not
// Singleton) sidesteps both: it's resolved from the same app-level DI scope as everything else in
// the request/job, the same way TenantSessionConnectionInterceptor already does for ITenantContext.
public sealed class DomainEventDispatchInterceptor(IAuditLogEntryProjector projector) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Project(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Project(DbContext context)
    {
        var aggregatesWithEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        if (aggregatesWithEvents.Count == 0)
        {
            return;
        }

        var domainEvents = aggregatesWithEvents.SelectMany(entity => entity.DomainEvents).ToList();
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            var entry = projector.Project(domainEvent);
            if (entry is not null)
            {
                context.Add(entry);
            }
        }
    }
}
