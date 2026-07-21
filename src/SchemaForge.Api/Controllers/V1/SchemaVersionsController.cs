using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Api.Middleware;
using SchemaForge.Application.Schemas.Commands.AddSchemaNode;
using SchemaForge.Application.Schemas.Commands.CreateDraftFromSuggestion;
using SchemaForge.Application.Schemas.Commands.CreateSchemaVersion;
using SchemaForge.Application.Schemas.Commands.DeprecateSchemaVersion;
using SchemaForge.Application.Schemas.Commands.ImportSchemaVersion;
using SchemaForge.Application.Schemas.Commands.MoveSchemaNode;
using SchemaForge.Application.Schemas.Commands.PublishSchemaVersion;
using SchemaForge.Application.Schemas.Commands.RemoveSchemaNode;
using SchemaForge.Application.Schemas.Commands.UpdateSchemaNode;
using SchemaForge.Application.Schemas.Queries.GetSchemaDiff;
using SchemaForge.Application.Schemas.Queries.GetSchemaVersion;
using SchemaForge.Application.Schemas.Queries.GetSchemaVersionDocumentation;
using SchemaForge.Application.Schemas.Queries.GetSchemaVersionExport;
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
    [Idempotent]
    public async Task<IActionResult> Create(
        Guid schemaId, CreateSchemaVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(schemaId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    // Raw JSON Schema document as the body (matching /validate's [FromBody] JsonElement pattern),
    // bumpKind/changeSummary as query params - the same "create a new Draft" shape as
    // POST .../versions, just populated from an existing document instead of starting empty.
    [HttpPost("api/v1/schemas/{schemaId:guid}/import")]
    [Idempotent]
    public async Task<IActionResult> Import(
        Guid schemaId, [FromBody] JsonElement schemaDocument, [FromQuery] VersionBumpKind bumpKind,
        [FromQuery] string? changeSummary, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ImportSchemaVersionCommand(schemaId, schemaDocument, bumpKind.ToDomain(), changeSummary), cancellationToken);
        return result.ToActionResult(r => new CreateSchemaVersionResponse(r.SchemaVersionId, r.VersionNumber.ToString()));
    }

    // Step 9 §2: the suggestion itself is never persisted, so the client resends it in full
    // (round-tripped from suggest-schema's response) along with which node ids were accepted -
    // materializing them onto a real Draft via the exact same domain methods CreateSchemaVersion/
    // AddNode already use, so every aggregate invariant applies identically here.
    [HttpPost("api/v1/schemas/{schemaId:guid}/versions/from-suggestion")]
    [Idempotent]
    public async Task<IActionResult> CreateFromSuggestion(
        Guid schemaId, CreateDraftFromSuggestionRequest request, CancellationToken cancellationToken)
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
        if (result.IsSuccess)
        {
            Response.SetETag(result.Value.RowVersion);
        }

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
        if (!Request.TryGetIfMatch(out var expectedVersion))
        {
            return ConcurrencyExtensions.PreconditionRequired();
        }

        var result = await sender.Send(
            new UpdateSchemaNodeCommand(schemaVersionId, nodeId, request.ToDomain(), expectedVersion), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/schema-versions/{schemaVersionId:guid}/nodes/{nodeId:guid}")]
    public async Task<IActionResult> RemoveNode(Guid schemaVersionId, Guid nodeId, CancellationToken cancellationToken)
    {
        if (!Request.TryGetIfMatch(out var expectedVersion))
        {
            return ConcurrencyExtensions.PreconditionRequired();
        }

        var result = await sender.Send(new RemoveSchemaNodeCommand(schemaVersionId, nodeId, expectedVersion), cancellationToken);
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
    [Idempotent]
    public async Task<IActionResult> Publish(Guid schemaVersionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishSchemaVersionCommand(schemaVersionId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/schema-versions/{schemaVersionId:guid}/deprecate")]
    [Idempotent]
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

    private static readonly Dictionary<string, string> ExportContentTypes = new()
    {
        ["json-schema"] = "application/schema+json",
        ["openapi"] = "application/json",
        ["typescript"] = "text/plain",
        ["csharp"] = "text/plain",
    };

    [HttpGet("api/v1/schema-versions/{schemaVersionId:guid}/export")]
    public async Task<IActionResult> Export(
        Guid schemaVersionId, [FromQuery] string format, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSchemaVersionExportQuery(schemaVersionId, format), cancellationToken);
        return result.ToContentActionResult(ExportContentTypes.GetValueOrDefault(format, "text/plain"));
    }

    private static readonly Dictionary<string, string> DocumentationContentTypes = new()
    {
        ["html"] = "text/html",
        ["markdown"] = "text/markdown",
        ["json"] = "application/json",
    };

    [HttpGet("api/v1/schema-versions/{schemaVersionId:guid}/documentation")]
    public async Task<IActionResult> Documentation(
        Guid schemaVersionId, [FromQuery] string format, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSchemaVersionDocumentationQuery(schemaVersionId, format), cancellationToken);
        return result.ToContentActionResult(DocumentationContentTypes.GetValueOrDefault(format, "text/plain"));
    }
}
