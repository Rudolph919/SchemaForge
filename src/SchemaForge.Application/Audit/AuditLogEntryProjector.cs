using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Audit;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Audit;

public sealed class AuditLogEntryProjector(ITenantContext tenantContext, ICurrentUserContext currentUserContext)
    : IAuditLogEntryProjector
{
    public AuditLogEntry? Project(IDomainEvent domainEvent)
    {
        if (domainEvent is not IAuditableDomainEvent auditable)
        {
            return null;
        }

        var organizationId = tenantContext.CurrentTenantId;
        var actorUserId = currentUserContext.UserId;

        // No ambient tenant/actor is a real, expected case for pre-auth flows (registration,
        // before any Organization or JWT exists) - simply not audited rather than persisted with
        // a fabricated placeholder actor/org.
        if (organizationId is null || actorUserId is null)
        {
            return null;
        }

        return AuditLogEntry.Record(
            organizationId.Value, actorUserId.Value, auditable.Action, auditable.EntityType, auditable.EntityId,
            auditable.MetadataJson, auditable.OccurredAt);
    }
}
