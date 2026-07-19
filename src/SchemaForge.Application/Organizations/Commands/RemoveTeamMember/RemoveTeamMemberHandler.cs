using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.RemoveTeamMember;

public sealed class RemoveTeamMemberHandler(ITeamRepository teamRepository)
    : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);

        if (team is null)
        {
            return Result.Failure(Error.NotFound("Team.NotFound", "No such team."));
        }

        return team.RemoveMember(request.UserId);
    }
}
