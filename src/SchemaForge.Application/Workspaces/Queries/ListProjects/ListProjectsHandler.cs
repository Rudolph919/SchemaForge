using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Queries.ListProjects;

public sealed class ListProjectsHandler(IProjectRepository projectRepository)
    : IRequestHandler<ListProjectsQuery, Result<IReadOnlyList<ProjectSummary>>>
{
    public async Task<Result<IReadOnlyList<ProjectSummary>>> Handle(
        ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var projects = await projectRepository.GetAllForCurrentOrganizationAsync(cancellationToken);

        var summaries = projects
            .Select(p => new ProjectSummary(p.Id, p.Name, p.Description, p.Status))
            .ToList();

        return Result<IReadOnlyList<ProjectSummary>>.Success(summaries);
    }
}
