using MediatR;
using SchemaForge.Domain.Audit;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Audit.Queries.GetAuditLog;

public sealed class GetAuditLogHandler(IAuditLogEntryRepository auditLogEntryRepository)
    : IRequestHandler<GetAuditLogQuery, Result<AuditLogPage>>
{
    public async Task<Result<AuditLogPage>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var criteria = new AuditLogSearchCriteria(
            request.EntityType, request.EntityId, request.ActorUserId, request.OccurredFrom, request.OccurredTo,
            request.Page, request.PageSize);

        var page = await auditLogEntryRepository.SearchAsync(criteria, cancellationToken);

        return new AuditLogPage(page.Items.Select(ToSummary).ToList(), page.TotalCount, request.Page, request.PageSize);
    }

    private static AuditLogEntrySummary ToSummary(AuditLogEntry entry) => new(
        entry.Id, entry.ActorUserId, entry.Action, entry.EntityType, entry.EntityId, entry.MetadataJson, entry.OccurredAt);
}
