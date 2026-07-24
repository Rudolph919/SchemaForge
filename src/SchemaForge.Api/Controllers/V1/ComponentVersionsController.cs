using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Api.Middleware;
using SchemaForge.Application.Components.Commands.AddComponentLocalDefinition;
using SchemaForge.Application.Components.Commands.AddComponentNode;
using SchemaForge.Application.Components.Commands.CreateComponentVersion;
using SchemaForge.Application.Components.Commands.DeprecateComponentVersion;
using SchemaForge.Application.Components.Commands.MoveComponentNode;
using SchemaForge.Application.Components.Commands.PublishComponentVersion;
using SchemaForge.Application.Components.Commands.RemoveComponentLocalDefinition;
using SchemaForge.Application.Components.Commands.RemoveComponentNode;
using SchemaForge.Application.Components.Commands.ReparentComponentNode;
using SchemaForge.Application.Components.Commands.UpdateComponentNode;
using SchemaForge.Application.Components.Queries.GetComponentVersion;
using SchemaForge.Application.Components.Queries.ListComponentVersions;
using SchemaForge.Contracts.V1.Components;
using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Api.Controllers.V1;

// No /validate or /validation-runs routes - ValidationRun is a Schema-specific concept
// (Step 4 §7: a payload is validated against a schema a consumer actually authored data for,
// never directly against a reusable component fragment).
[ApiController]
[Authorize]
public sealed class ComponentVersionsController(ISender sender) : ControllerBase
{
    [HttpPost("api/v1/components/{componentId:guid}/versions")]
    [Idempotent]
    public async Task<IActionResult> Create(
        Guid componentId, CreateComponentVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(componentId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpGet("api/v1/components/{componentId:guid}/versions")]
    public async Task<IActionResult> List(Guid componentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListComponentVersionsQuery(componentId), cancellationToken);
        return result.ToActionResult(versions => versions.Select(v => v.ToResponse()).ToList());
    }

    [HttpGet("api/v1/component-versions/{componentVersionId:guid}")]
    public async Task<IActionResult> Get(Guid componentVersionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetComponentVersionQuery(componentVersionId), cancellationToken);
        if (result.IsSuccess)
        {
            Response.SetETag(result.Value.RowVersion);
        }

        return result.ToActionResult(d => d.ToResponse());
    }

    [HttpPost("api/v1/component-versions/{componentVersionId:guid}/nodes")]
    public async Task<IActionResult> AddNode(
        Guid componentVersionId, AddComponentNodeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(componentVersionId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpPatch("api/v1/component-versions/{componentVersionId:guid}/nodes/{nodeId:guid}")]
    public async Task<IActionResult> UpdateNode(
        Guid componentVersionId, Guid nodeId, UpdateSchemaNodeRequest request, CancellationToken cancellationToken)
    {
        if (!Request.TryGetIfMatch(out var expectedVersion))
        {
            return ConcurrencyExtensions.PreconditionRequired();
        }

        var result = await sender.Send(
            new UpdateComponentNodeCommand(componentVersionId, nodeId, request.ToDomain(), expectedVersion),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/component-versions/{componentVersionId:guid}/nodes/{nodeId:guid}")]
    public async Task<IActionResult> RemoveNode(Guid componentVersionId, Guid nodeId, CancellationToken cancellationToken)
    {
        if (!Request.TryGetIfMatch(out var expectedVersion))
        {
            return ConcurrencyExtensions.PreconditionRequired();
        }

        var result = await sender.Send(
            new RemoveComponentNodeCommand(componentVersionId, nodeId, expectedVersion), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/component-versions/{componentVersionId:guid}/local-definitions")]
    public async Task<IActionResult> AddLocalDefinition(
        Guid componentVersionId, AddLocalDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToAddComponentLocalDefinitionCommand(componentVersionId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpDelete("api/v1/component-versions/{componentVersionId:guid}/local-definitions/{localDefinitionId:guid}")]
    public async Task<IActionResult> RemoveLocalDefinition(
        Guid componentVersionId, Guid localDefinitionId, CancellationToken cancellationToken)
    {
        if (!Request.TryGetIfMatch(out var expectedVersion))
        {
            return ConcurrencyExtensions.PreconditionRequired();
        }

        var result = await sender.Send(
            new RemoveComponentLocalDefinitionCommand(componentVersionId, localDefinitionId, expectedVersion),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/component-versions/{componentVersionId:guid}/nodes/{nodeId:guid}/move")]
    public async Task<IActionResult> MoveNode(
        Guid componentVersionId, Guid nodeId, MoveSchemaNodeRequest request, CancellationToken cancellationToken)
    {
        if (request.NewParentNodeId is { } newParentNodeId)
        {
            var reparentResult = await sender.Send(
                request.ToReparentComponentNodeCommand(componentVersionId, nodeId, newParentNodeId), cancellationToken);
            return reparentResult.ToActionResult();
        }

        var result = await sender.Send(request.ToMoveComponentNodeCommand(componentVersionId, nodeId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/component-versions/{componentVersionId:guid}/publish")]
    [Idempotent]
    public async Task<IActionResult> Publish(Guid componentVersionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishComponentVersionCommand(componentVersionId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/component-versions/{componentVersionId:guid}/deprecate")]
    [Idempotent]
    public async Task<IActionResult> Deprecate(Guid componentVersionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeprecateComponentVersionCommand(componentVersionId), cancellationToken);
        return result.ToActionResult();
    }
}
