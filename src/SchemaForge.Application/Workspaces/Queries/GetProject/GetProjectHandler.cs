using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Queries.GetProject;

public sealed class GetProjectHandler(IProjectRepository projectRepository)
    : IRequestHandler<GetProjectQuery, Result<ProjectDetail>>
{
    public async Task<Result<ProjectDetail>> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<ProjectDetail>.Failure(Error.NotFound("Project.NotFound", "No such project."));
        }

        return new ProjectDetail(project.Id, project.Name, project.Description, project.Status);
    }
}
