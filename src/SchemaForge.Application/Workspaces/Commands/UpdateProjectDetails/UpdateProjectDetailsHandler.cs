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

        // Only checked when the name is actually changing - renaming to a project's own current
        // name must stay a harmless no-op, not a false conflict. Without this check, renaming
        // into an already-taken name fell through to the DB unique index and crashed with a raw
        // 500 instead of a clean 409 - caught live while building the equivalent check for
        // SchemaDefinition, which shares this exact handler shape.
        if (!string.Equals(project.Name, request.Name, StringComparison.Ordinal)
            && await projectRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            return Result.Failure(Error.Conflict(
                "Project.NameAlreadyExists", "A project with this name already exists in this organization."));
        }

        var renameResult = project.Rename(request.Name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        project.UpdateDescription(request.Description);
        projectRepository.ApplyExpectedVersion(project, request.ExpectedVersion);

        return Result.Success();
    }
}
