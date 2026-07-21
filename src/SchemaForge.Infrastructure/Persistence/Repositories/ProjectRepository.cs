using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Workspaces;
using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository(SchemaForgeDbContext dbContext) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Projects.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetAllForCurrentOrganizationAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Projects.ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Projects.AnyAsync(p => p.Name == name, cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken) =>
        await dbContext.Projects.AddAsync(project, cancellationToken);

    public void ApplyExpectedVersion(Project project, uint expectedVersion) =>
        dbContext.ApplyExpectedVersion(project, expectedVersion);
}
