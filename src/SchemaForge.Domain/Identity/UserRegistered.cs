using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Identity;

// Not auditable in practice even though it implements the contract: registration happens before
// any Organization or JWT exists, so AuditLogEntryProjector's ambient tenant/actor lookup finds
// neither and skips it (see its own comment) - implementing IAuditableDomainEvent costs nothing
// and means this starts getting audited for free the moment there's ever an ambient context to
// attribute it to (e.g. an admin registering a user on someone else's behalf, if that's ever
// added).
public sealed record UserRegistered(Guid UserId, string Email) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "User.Registered";
    public string EntityType => "User";
    public Guid EntityId => UserId;
    public string? MetadataJson => JsonSerializer.Serialize(new { Email });
}
