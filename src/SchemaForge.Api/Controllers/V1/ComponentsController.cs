using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Api.Middleware;
using SchemaForge.Application.Components.Commands.CreateComponentDefinition;
using SchemaForge.Application.Components.Commands.UpdateComponentDefinitionDetails;
using SchemaForge.Application.Components.Queries.GetComponentDefinition;
using SchemaForge.Application.Components.Queries.GetComponentLibrary;
using SchemaForge.Contracts.V1.Components;

namespace SchemaForge.Api.Controllers.V1;

// No {organizationId} route segment, unlike the architecture doc's illustrative
// /organizations/{orgId}/components sketch - every other org-scoped "list mine" route in this
// Api (/api/v1/projects, /api/v1/teams, /api/v1/members) derives the organization from the JWT's
// ambient tenant context rather than a path parameter, and GetComponentLibraryQuery was already
// built parameterless in the previous PR to match. A redundant org id in the path would just be
// something the client could get out of sync with its own token.
[ApiController]
[Authorize]
[Route("api/v1/components")]
public sealed class ComponentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetComponentLibraryQuery(), cancellationToken);
        return result.ToActionResult(components => components.Select(c => c.ToResponse()).ToList());
    }

    [HttpPost]
    [Idempotent]
    public async Task<IActionResult> Create(CreateComponentDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpGet("{componentDefinitionId:guid}")]
    public async Task<IActionResult> Get(Guid componentDefinitionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetComponentDefinitionQuery(componentDefinitionId), cancellationToken);
        return result.ToActionResult(d => d.ToResponse());
    }

    [HttpPatch("{componentDefinitionId:guid}")]
    public async Task<IActionResult> UpdateDetails(
        Guid componentDefinitionId, UpdateComponentDefinitionDetailsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(componentDefinitionId), cancellationToken);
        return result.ToActionResult();
    }
}
