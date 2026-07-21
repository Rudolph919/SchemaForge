using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Application.Workspaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> GetAllForCurrentOrganizationAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(Project project, CancellationToken cancellationToken);

    // Step 6 §1.5: sets the tracked entity's expected concurrency-token value from a client's
    // If-Match header, so the next SaveChangesAsync fails with a conflict if the row has since
    // changed underneath it.
    void ApplyExpectedVersion(Project project, uint expectedVersion);
}
