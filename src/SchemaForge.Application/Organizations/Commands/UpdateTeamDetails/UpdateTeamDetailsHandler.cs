using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.UpdateTeamDetails;

public sealed class UpdateTeamDetailsHandler(ITeamRepository teamRepository)
    : IRequestHandler<UpdateTeamDetailsCommand, Result>
{
    public async Task<Result> Handle(UpdateTeamDetailsCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);

        if (team is null)
        {
            return Result.Failure(Error.NotFound("Team.NotFound", "No such team."));
        }

        // Only checked when the name is actually changing - see UpdateProjectDetailsHandler for
        // why (same latent bug, same fix, found live while building the SchemaDefinition
        // equivalent of this handler).
        if (!string.Equals(team.Name, request.Name, StringComparison.Ordinal)
            && await teamRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            return Result.Failure(Error.Conflict(
                "Team.NameAlreadyExists", "A team with this name already exists in this organization."));
        }

        var renameResult = team.Rename(request.Name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        team.UpdateDescription(request.Description);

        return Result.Success();
    }
}
