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

        var renameResult = team.Rename(request.Name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        team.UpdateDescription(request.Description);

        return Result.Success();
    }
}
