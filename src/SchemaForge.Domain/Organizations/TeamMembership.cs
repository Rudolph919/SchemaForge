using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

// Child entity of Team, not an independent aggregate - team rosters stay small (Step 3 Ground
// Rule 2), unlike OrganizationMembership which is its own aggregate root.
public sealed class TeamMembership : Entity<Guid>
{
    public Guid UserId { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    private TeamMembership() { } // EF Core materialization

    private TeamMembership(Guid id, Guid userId, DateTimeOffset joinedAt) : base(id)
    {
        UserId = userId;
        JoinedAt = joinedAt;
    }

    internal static TeamMembership Create(Guid userId) => new(Guid.NewGuid(), userId, DateTimeOffset.UtcNow);
}
