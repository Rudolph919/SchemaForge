using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Application.Schemas.Commands.AddSchemaNode;
using SchemaForge.Application.Schemas.Commands.CreateSchemaVersion;
using SchemaForge.Application.Schemas.Commands.DeprecateSchemaVersion;
using SchemaForge.Application.Schemas.Commands.MoveSchemaNode;
using SchemaForge.Application.Schemas.Commands.PublishSchemaVersion;
using SchemaForge.Application.Schemas.Commands.RemoveSchemaNode;
using SchemaForge.Application.Schemas.Commands.UpdateSchemaNode;
using SchemaForge.Application.Schemas.Queries.GetSchemaDiff;
using SchemaForge.Application.Schemas.Queries.GetSchemaVersion;
using SchemaForge.Application.Schemas.Queries.ListSchemaVersions;
using SchemaForge.Application.Validation.Commands.ValidateJsonPayload;
using SchemaForge.Application.Validation.Queries.ListValidationRuns;
using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
public sealed class SchemaVersionsController(ISender sender) : ControllerBase
{
    [HttpPost("api/v1/schemas/{schemaId:guid}/versions")]
    public async Task<IActionResult> Create(
        Guid schemaId, CreateSchemaVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(schemaId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpGet("api/v1/schemas/{schemaId:guid}/versions")]
    public async Task<IActionResult> List(Guid schemaId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListSchemaVersionsQuery(schemaId), cancellationToken);
        return result.ToActionResult(versions => versions.Select(v => v.ToResponse()).ToList());
    }

    [HttpGet("api/v1/schema-versions/{schemaVersionId:guid}")]
    public async Task<IActionResult> Get(Guid schemaVersionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSchemaVersionQuery(schemaVersionId), cancellationToken);
        return result.ToActionResult(d => d.ToResponse());
    }

    [HttpPost("api/v1/schema-versions/{schemaVersionId:guid}/nodes")]
    public async Task<IActionResult> AddNode(
        Guid schemaVersionId, AddSchemaNodeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(schemaVersionId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpPatch("api/v1/schema-versions/{schemaVersionId:guid}/nodes/{nodeId:guid}")]
    public async Task<IActionResult> UpdateNode(
        Guid schemaVersionId, Guid nodeId, UpdateSchemaNodeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateSchemaNodeCommand(schemaVersionId, nodeId, request.ToDomain()), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/schema-versions/{schemaVersionId:guid}/nodes/{nodeId:guid}")]
    public async Task<IActionResult> RemoveNode(Guid schemaVersionId, Guid nodeId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveSchemaNodeCommand(schemaVersionId, nodeId), cancellationToken);
        return result.ToActionResult();
    }

    // Reorders among existing siblings only - reparenting to a different node is a materially
    // riskier operation deferred out of Phase 2a (SchemaVersion.MoveNode's own scope note).
    [HttpPost("api/v1/schema-versions/{schemaVersionId:guid}/nodes/{nodeId:guid}/move")]
    public async Task<IActionResult> MoveNode(
        Guid schemaVersionId, Guid nodeId, MoveSchemaNodeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(schemaVersionId, nodeId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/schema-versions/{schemaVersionId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid schemaVersionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishSchemaVersionCommand(schemaVersionId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/schema-versions/{schemaVersionId:guid}/deprecate")]
    public async Task<IActionResult> Deprecate(Guid schemaVersionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeprecateSchemaVersionCommand(schemaVersionId), cancellationToken);
        return result.ToActionResult();
    }

    // 200 OK regardless of valid/invalid outcome (Step 6 §1.4) - ToActionResult already does this
    // since it only distinguishes on Result.IsSuccess, not on the validation Outcome inside the payload.
    [HttpPost("api/v1/schema-versions/{schemaVersionId:guid}/validate")]
    public async Task<IActionResult> Validate(
        Guid schemaVersionId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ValidateJsonPayloadCommand(schemaVersionId, payload), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpGet("api/v1/schema-versions/{schemaVersionId:guid}/validation-runs")]
    public async Task<IActionResult> ListValidationRuns(Guid schemaVersionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListValidationRunsQuery(schemaVersionId), cancellationToken);
        return result.ToActionResult(runs => runs.Select(r => r.ToResponse()).ToList());
    }

    [HttpGet("api/v1/schema-versions/{schemaVersionId:guid}/diff")]
    public async Task<IActionResult> Diff(Guid schemaVersionId, [FromQuery] Guid against, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSchemaDiffQuery(schemaVersionId, against), cancellationToken);
        return result.ToActionResult(d => d.ToResponse());
    }
}
