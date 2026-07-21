using SchemaForge.Domain.Audit;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Audit;

// Pure translation, no I/O of its own - DomainEventDispatchInterceptor calls this synchronously,
// inside the same SaveChangesAsync call that's already in flight (see the interceptor's own
// comment for why: an independent second SaveChangesAsync call on the same DbContext instance
// corrupted Npgsql's connection state, confirmed by the integration suite actually breaking when
// that was tried). Returns null for events that aren't IAuditableDomainEvent, or for genuinely
// pre-auth events (registration) where there's no ambient tenant/actor yet to attribute one to.
public interface IAuditLogEntryProjector
{
    AuditLogEntry? Project(IDomainEvent domainEvent);
}
