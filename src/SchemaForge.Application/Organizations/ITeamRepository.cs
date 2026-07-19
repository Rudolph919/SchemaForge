using SchemaForge.Domain.Organizations;

namespace SchemaForge.Application.Organizations;

public interface ITeamRepository
{
    // Normal tenant-scoped lookup - Team has no self-lookup-before-tenant-context problem the
    // way OrganizationMembership does, so one method suffices here.
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamSummary>> GetAllForCurrentOrganizationAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(Team team, CancellationToken cancellationToken);
}

public sealed record TeamSummary(Guid Id, string Name, string? Description, int MemberCount);
