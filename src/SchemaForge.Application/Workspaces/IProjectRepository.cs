using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Application.Workspaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> GetAllForCurrentOrganizationAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(Project project, CancellationToken cancellationToken);
}
