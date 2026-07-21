namespace SchemaForge.Contracts.V1.Audit;

public sealed record AuditLogEntryResponse(
    Guid Id, Guid ActorUserId, string Action, string EntityType, Guid EntityId, string? MetadataJson,
    DateTimeOffset OccurredAt);

public sealed record AuditLogPageResponse(IReadOnlyList<AuditLogEntryResponse> Items, int TotalCount, int Page, int PageSize);
