using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class TeamRepository(SchemaForgeDbContext dbContext) : ITeamRepository
{
    // Members is an EF Core owned collection (Step 1's Infrastructure PR), loaded automatically
    // with its owner - no explicit .Include() needed.
    public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Teams.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TeamSummary>> GetAllForCurrentOrganizationAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Teams
            .Select(t => new TeamSummary(t.Id, t.Name, t.Description, t.Members.Count))
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Teams.AnyAsync(t => t.Name == name, cancellationToken);

    public async Task AddAsync(Team team, CancellationToken cancellationToken) =>
        await dbContext.Teams.AddAsync(team, cancellationToken);
}
