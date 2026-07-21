using System.Text.Json;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed record TeamMemberRemoved(Guid TeamId, Guid UserId) : IAuditableDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public string Action => "Team.MemberRemoved";
    public string EntityType => "Team";
    public Guid EntityId => TeamId;
    public string? MetadataJson => JsonSerializer.Serialize(new { UserId });
}
