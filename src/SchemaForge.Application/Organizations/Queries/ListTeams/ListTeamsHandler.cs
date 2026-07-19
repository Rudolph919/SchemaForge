using MediatR;
using SchemaForge.Application.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Queries.ListTeams;

public sealed class ListTeamsHandler(ITeamRepository teamRepository)
    : IRequestHandler<ListTeamsQuery, Result<IReadOnlyList<TeamSummary>>>
{
    public async Task<Result<IReadOnlyList<TeamSummary>>> Handle(
        ListTeamsQuery request, CancellationToken cancellationToken)
    {
        var teams = await teamRepository.GetAllForCurrentOrganizationAsync(cancellationToken);
        return Result<IReadOnlyList<TeamSummary>>.Success(teams);
    }
}
