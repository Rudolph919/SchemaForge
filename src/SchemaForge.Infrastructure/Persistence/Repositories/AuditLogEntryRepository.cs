using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Audit;
using SchemaForge.Domain.Audit;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class AuditLogEntryRepository(SchemaForgeDbContext dbContext) : IAuditLogEntryRepository
{
    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken) =>
        await dbContext.AuditLogEntries.AddAsync(entry, cancellationToken);

    public async Task<PagedAuditLogEntries> SearchAsync(
        AuditLogSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogEntries.AsQueryable();

        if (criteria.EntityType is not null)
        {
            query = query.Where(e => e.EntityType == criteria.EntityType);
        }

        if (criteria.EntityId is not null)
        {
            query = query.Where(e => e.EntityId == criteria.EntityId);
        }

        if (criteria.ActorUserId is not null)
        {
            query = query.Where(e => e.ActorUserId == criteria.ActorUserId);
        }

        if (criteria.OccurredFrom is not null)
        {
            query = query.Where(e => e.OccurredAt >= criteria.OccurredFrom);
        }

        if (criteria.OccurredTo is not null)
        {
            query = query.Where(e => e.OccurredAt <= criteria.OccurredTo);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedAuditLogEntries(items, totalCount);
    }
}
