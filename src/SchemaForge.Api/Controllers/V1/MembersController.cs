using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Application.Organizations.Commands.AcceptInvitation;
using SchemaForge.Application.Organizations.Commands.RevokeMember;
using SchemaForge.Application.Organizations.Queries.ListMembers;
using SchemaForge.Contracts.V1.Organizations;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/organizations/members")]
public sealed class MembersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListOrganizationMembersQuery(), cancellationToken);
        return result.ToActionResult(members => members.Select(m => m.ToResponse()).ToList());
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite(InviteMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpPost("{membershipId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid membershipId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AcceptOrganizationInvitationCommand(membershipId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{membershipId:guid}/role")]
    public async Task<IActionResult> ChangeRole(
        Guid membershipId, ChangeMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(membershipId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{membershipId:guid}")]
    public async Task<IActionResult> Revoke(Guid membershipId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RevokeOrganizationMemberCommand(membershipId), cancellationToken);
        return result.ToActionResult();
    }
}
