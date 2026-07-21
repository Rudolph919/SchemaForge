using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Audit.Queries.GetAuditLog;

public sealed record GetAuditLogQuery(
    string? EntityType,
    Guid? EntityId,
    Guid? ActorUserId,
    DateTimeOffset? OccurredFrom,
    DateTimeOffset? OccurredTo,
    int Page,
    int PageSize) : IQuery<Result<AuditLogPage>>;

public sealed record AuditLogEntrySummary(
    Guid Id, Guid ActorUserId, string Action, string EntityType, Guid EntityId, string? MetadataJson,
    DateTimeOffset OccurredAt);

public sealed record AuditLogPage(IReadOnlyList<AuditLogEntrySummary> Items, int TotalCount, int Page, int PageSize);
