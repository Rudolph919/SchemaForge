using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Application.Organizations.Queries.ListMyMemberships;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/me/memberships")]
public sealed class MyMembershipsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListMyMembershipsQuery(), cancellationToken);
        return result.ToActionResult(memberships => memberships.Select(m => m.ToResponse()).ToList());
    }
}
