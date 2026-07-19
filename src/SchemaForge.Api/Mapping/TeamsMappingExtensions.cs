using SchemaForge.Application.Organizations;
using SchemaForge.Application.Organizations.Commands.CreateTeam;
using SchemaForge.Application.Organizations.Commands.UpdateTeamDetails;
using SchemaForge.Application.Organizations.Queries.GetTeam;
using SchemaForge.Contracts.V1.Teams;

namespace SchemaForge.Api.Mapping;

public static class TeamsMappingExtensions
{
    public static CreateTeamCommand ToCommand(this CreateTeamRequest request) =>
        new(request.Name, request.Description);

    public static CreateTeamResponse ToResponse(this CreateTeamResult result) => new(result.TeamId);

    public static UpdateTeamDetailsCommand ToCommand(this UpdateTeamDetailsRequest request, Guid teamId) =>
        new(teamId, request.Name, request.Description);

    public static TeamSummaryResponse ToResponse(this TeamSummary summary) =>
        new(summary.Id, summary.Name, summary.Description, summary.MemberCount);

    public static TeamDetailResponse ToResponse(this TeamDetail detail) =>
        new(detail.Id, detail.Name, detail.Description, detail.Members.Select(m => m.ToResponse()).ToList());

    private static TeamMemberResponse ToResponse(this TeamMemberDetail member) => new(member.UserId, member.JoinedAt);
}
