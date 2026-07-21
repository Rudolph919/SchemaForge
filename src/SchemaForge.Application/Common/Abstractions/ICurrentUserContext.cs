namespace SchemaForge.Application.Common.Abstractions;

// Parallel to ITenantContext, but for "who," not "which org." Needed the moment a command has to
// verify a resource actually belongs to the caller (e.g. accepting an invitation must not let
// caller A accept an invitation addressed to user B just because they know its id).
public interface ICurrentUserContext
{
    Guid? UserId { get; }

    // Parallel to ITenantContext.SetTenant - lets a background job (no HttpContext to resolve
    // from, same gap documented on ITestRunExecutor) tell the ambient context who it's acting on
    // behalf of, so AuditLogEntryProjector can still attribute an ActorUserId to events the job
    // raises instead of finding no ambient user at all.
    void SetUser(Guid userId);
}
