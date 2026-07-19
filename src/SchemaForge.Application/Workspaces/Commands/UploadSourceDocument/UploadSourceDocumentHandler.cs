using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Workspaces;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.UploadSourceDocument;

public sealed class UploadSourceDocumentHandler(
    IProjectRepository projectRepository,
    ISourceDocumentRepository sourceDocumentRepository,
    IFileStorage fileStorage,
    ITenantContext tenantContext)
    : IRequestHandler<UploadSourceDocumentCommand, Result<UploadSourceDocumentResult>>
{
    public async Task<Result<UploadSourceDocumentResult>> Handle(
        UploadSourceDocumentCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<UploadSourceDocumentResult>.Failure(
                Error.NotFound("Project.NotFound", "No such project."));
        }

        var organizationId = tenantContext.CurrentTenantId!.Value;

        // Key is opaque to IFileStorage (Step 4) - a fresh guid here, independent of the
        // SourceDocument's own id, since the storage write has to happen before the domain
        // object (and therefore its id) exists. Uploaded before the row is written: a failed
        // SaveChanges afterward leaves an orphaned blob, not a DB row pointing at nothing.
        var storageKey = $"{organizationId}/{request.ProjectId}/{Guid.NewGuid()}/{request.FileName}";

        await fileStorage.UploadAsync(storageKey, request.Content, request.ContentType, cancellationToken);

        var document = SourceDocument.Create(
            organizationId, request.ProjectId, request.FileName, storageKey, request.ContentType,
            request.SizeBytes);

        await sourceDocumentRepository.AddAsync(document, cancellationToken);

        return new UploadSourceDocumentResult(document.Id);
    }
}
