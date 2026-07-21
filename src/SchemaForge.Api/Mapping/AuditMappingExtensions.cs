using SchemaForge.Application.Audit.Queries.GetAuditLog;
using SchemaForge.Contracts.V1.Audit;

namespace SchemaForge.Api.Mapping;

public static class AuditMappingExtensions
{
    public static AuditLogPageResponse ToResponse(this AuditLogPage page) => new(
        page.Items.Select(ToResponse).ToList(), page.TotalCount, page.Page, page.PageSize);

    private static AuditLogEntryResponse ToResponse(this AuditLogEntrySummary entry) => new(
        entry.Id, entry.ActorUserId, entry.Action, entry.EntityType, entry.EntityId, entry.MetadataJson, entry.OccurredAt);
}
