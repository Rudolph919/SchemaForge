using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.ArchiveProject;

public sealed class ArchiveProjectHandler(IProjectRepository projectRepository)
    : IRequestHandler<ArchiveProjectCommand, Result>
{
    public async Task<Result> Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure(Error.NotFound("Project.NotFound", "No such project."));
        }

        return project.Archive();
    }
}
