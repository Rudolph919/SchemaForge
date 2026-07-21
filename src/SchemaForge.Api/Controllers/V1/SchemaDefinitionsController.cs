using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Api.Middleware;
using SchemaForge.Application.Schemas.Commands.CreateSchemaDefinition;
using SchemaForge.Application.Schemas.Commands.UpdateSchemaDefinitionDetails;
using SchemaForge.Application.Schemas.Queries.GetSchemaDefinition;
using SchemaForge.Application.Schemas.Queries.GetSchemaLibrary;
using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
public sealed class SchemaDefinitionsController(ISender sender) : ControllerBase
{
    [HttpGet("api/v1/projects/{projectId:guid}/schemas")]
    public async Task<IActionResult> List(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSchemaLibraryQuery(projectId), cancellationToken);
        return result.ToActionResult(schemas => schemas.Select(s => s.ToResponse()).ToList());
    }

    [HttpPost("api/v1/projects/{projectId:guid}/schemas")]
    [Idempotent]
    public async Task<IActionResult> Create(
        Guid projectId, CreateSchemaDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(projectId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpGet("api/v1/schemas/{schemaDefinitionId:guid}")]
    public async Task<IActionResult> Get(Guid schemaDefinitionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSchemaDefinitionQuery(schemaDefinitionId), cancellationToken);
        if (result.IsSuccess)
        {
            Response.SetETag(result.Value.RowVersion);
        }

        return result.ToActionResult(d => d.ToResponse());
    }

    [HttpPatch("api/v1/schemas/{schemaDefinitionId:guid}")]
    public async Task<IActionResult> UpdateDetails(
        Guid schemaDefinitionId, UpdateSchemaDefinitionDetailsRequest request, CancellationToken cancellationToken)
    {
        if (!Request.TryGetIfMatch(out var expectedVersion))
        {
            return ConcurrencyExtensions.PreconditionRequired();
        }

        var result = await sender.Send(request.ToCommand(schemaDefinitionId, expectedVersion), cancellationToken);
        return result.ToActionResult();
    }
}
