using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record TeamMemberAdded(Guid TeamId, Guid UserId) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "Team.MemberAdded";
    public string EntityType => "Team";
    public Guid EntityId => TeamId;
    public string? MetadataJson => JsonSerializer.Serialize(new { UserId });
}
