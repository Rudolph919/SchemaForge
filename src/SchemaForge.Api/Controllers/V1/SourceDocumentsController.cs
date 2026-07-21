using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Api.Middleware;
using SchemaForge.Application.Workspaces.Commands.DeleteSourceDocument;
using SchemaForge.Application.Workspaces.Commands.UploadSourceDocument;
using SchemaForge.Application.Workspaces.Queries.ListSourceDocuments;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/projects/{projectId:guid}/documents")]
public sealed class SourceDocumentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListSourceDocumentsQuery(projectId), cancellationToken);
        return result.ToActionResult(documents => documents.Select(d => d.ToResponse()).ToList());
    }

    [HttpPost]
    [Idempotent]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid projectId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        var command = new UploadSourceDocumentCommand(
            projectId, file.FileName, file.ContentType, file.Length, stream);
        var result = await sender.Send(command, cancellationToken);

        return result.ToActionResult(r => r.ToResponse());
    }

    // Absolute route, not nested under {projectId}: DeleteSourceDocumentCommand only needs the
    // document's own id (already tenant-scoped via RLS), so a projectId segment here would be
    // present in the URL but silently ignored - worse than just not having it.
    [HttpDelete("/api/v1/documents/{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteSourceDocumentCommand(documentId), cancellationToken);
        return result.ToActionResult();
    }
}
