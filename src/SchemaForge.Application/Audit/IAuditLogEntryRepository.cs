using SchemaForge.Domain.Audit;

namespace SchemaForge.Application.Audit;

public interface IAuditLogEntryRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken);

    Task<PagedAuditLogEntries> SearchAsync(AuditLogSearchCriteria criteria, CancellationToken cancellationToken);
}

// entityType/entityId/actorUserId are optional filters (Step 6 §2.8) - null means "don't filter
// on this dimension." occurredFrom/occurredTo bound the date range, both inclusive, either end
// optional.
public sealed record AuditLogSearchCriteria(
    string? EntityType,
    Guid? EntityId,
    Guid? ActorUserId,
    DateTimeOffset? OccurredFrom,
    DateTimeOffset? OccurredTo,
    int Page,
    int PageSize);

public sealed record PagedAuditLogEntries(IReadOnlyList<AuditLogEntry> Items, int TotalCount);
