using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed class Team : TenantOwnedAggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    private readonly List<TeamMembership> _members = [];

    public IReadOnlyList<TeamMembership> Members => _members.AsReadOnly();

    private Team() { } // EF Core materialization

    private Team(Guid id, Guid organizationId, string name, string? description) : base(id, organizationId)
    {
        Name = name;
        Description = description;
    }

    public static Team Create(Guid organizationId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Team name is required.", nameof(name));
        }

        var team = new Team(Guid.NewGuid(), organizationId, name, description);
        team.RaiseDomainEvent(new TeamCreated(organizationId, team.Id, name));

        return team;
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(Error.Validation("Team.NameRequired", "Team name is required."));
        }

        Name = newName;
        return Result.Success();
    }

    public void UpdateDescription(string? description) => Description = description;

    // Whether userId already holds an OrganizationMembership in this Team's Organization is a
    // cross-aggregate check the Application-layer command handler makes before calling this
    // method (Step 3 §4) - this method only enforces the invariant that's genuinely internal to
    // the Team aggregate itself: no duplicate membership.
    public Result AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            return Result.Failure(Error.Conflict(
                "Team.AlreadyMember", "This user is already a member of the team."));
        }

        _members.Add(TeamMembership.Create(userId));
        RaiseDomainEvent(new TeamMemberAdded(Id, userId));

        return Result.Success();
    }

    public Result RemoveMember(Guid userId)
    {
        var membership = _members.FirstOrDefault(m => m.UserId == userId);

        if (membership is null)
        {
            return Result.Failure(Error.NotFound(
                "Team.MembershipNotFound", "This user is not a member of the team."));
        }

        _members.Remove(membership);
        RaiseDomainEvent(new TeamMemberRemoved(Id, userId));

        return Result.Success();
    }
}
