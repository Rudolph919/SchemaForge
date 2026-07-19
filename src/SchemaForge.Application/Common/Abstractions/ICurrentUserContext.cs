namespace SchemaForge.Application.Common.Abstractions;

// Parallel to ITenantContext, but for "who," not "which org." Needed the moment a command has to
// verify a resource actually belongs to the caller (e.g. accepting an invitation must not let
// caller A accept an invitation addressed to user B just because they know its id).
public interface ICurrentUserContext
{
    Guid? UserId { get; }
}
