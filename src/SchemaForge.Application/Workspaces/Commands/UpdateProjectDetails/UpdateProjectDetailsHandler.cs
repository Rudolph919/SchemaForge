using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.UpdateProjectDetails;

public sealed class UpdateProjectDetailsHandler(IProjectRepository projectRepository)
    : IRequestHandler<UpdateProjectDetailsCommand, Result>
{
    public async Task<Result> Handle(UpdateProjectDetailsCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure(Error.NotFound("Project.NotFound", "No such project."));
        }

        var renameResult = project.Rename(request.Name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        project.UpdateDescription(request.Description);

        return Result.Success();
    }
}
