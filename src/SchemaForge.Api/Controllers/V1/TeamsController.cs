using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Api.Middleware;
using SchemaForge.Application.Organizations.Commands.AddTeamMember;
using SchemaForge.Application.Organizations.Commands.RemoveTeamMember;
using SchemaForge.Application.Organizations.Queries.GetTeam;
using SchemaForge.Application.Organizations.Queries.ListTeams;
using SchemaForge.Contracts.V1.Teams;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/teams")]
public sealed class TeamsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListTeamsQuery(), cancellationToken);
        return result.ToActionResult(teams => teams.Select(t => t.ToResponse()).ToList());
    }

    [HttpGet("{teamId:guid}")]
    public async Task<IActionResult> Get(Guid teamId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTeamQuery(teamId), cancellationToken);
        return result.ToActionResult(t => t.ToResponse());
    }

    [HttpPost]
    [Idempotent]
    public async Task<IActionResult> Create(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpPut("{teamId:guid}")]
    public async Task<IActionResult> UpdateDetails(
        Guid teamId, UpdateTeamDetailsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(teamId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{teamId:guid}/members")]
    public async Task<IActionResult> AddMember(
        Guid teamId, AddTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddTeamMemberCommand(teamId, request.UserId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveTeamMemberCommand(teamId, userId), cancellationToken);
        return result.ToActionResult();
    }
}
