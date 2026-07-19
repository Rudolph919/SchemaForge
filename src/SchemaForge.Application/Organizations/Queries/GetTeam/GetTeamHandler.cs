using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Queries.GetTeam;

public sealed class GetTeamHandler(ITeamRepository teamRepository) : IRequestHandler<GetTeamQuery, Result<TeamDetail>>
{
    public async Task<Result<TeamDetail>> Handle(GetTeamQuery request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);

        if (team is null)
        {
            return Result<TeamDetail>.Failure(Error.NotFound("Team.NotFound", "No such team."));
        }

        var members = team.Members
            .Select(m => new TeamMemberDetail(m.UserId, m.JoinedAt))
            .ToList();

        return new TeamDetail(team.Id, team.Name, team.Description, members);
    }
}
