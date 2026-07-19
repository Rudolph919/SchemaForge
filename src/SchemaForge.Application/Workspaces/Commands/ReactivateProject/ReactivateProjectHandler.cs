using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.ReactivateProject;

public sealed class ReactivateProjectHandler(IProjectRepository projectRepository)
    : IRequestHandler<ReactivateProjectCommand, Result>
{
    public async Task<Result> Handle(ReactivateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure(Error.NotFound("Project.NotFound", "No such project."));
        }

        return project.Reactivate();
    }
}
