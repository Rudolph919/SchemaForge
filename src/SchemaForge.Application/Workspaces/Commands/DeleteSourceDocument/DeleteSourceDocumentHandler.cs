using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.DeleteSourceDocument;

// The one legitimate hard-delete case (Step 6 §2.3) - source files are actually removed, not
// status-flipped. Storage and the DB row aren't in one transaction (TransactionBehavior only
// commits the DB side), so a mid-failure can't be made perfectly atomic without real saga/outbox
// machinery, which Step 1 §8 rules out as unwarranted weight for this. Deleting from storage
// first means the failure mode of a rare SaveChanges error afterward is a DB row surviving with
// a dangling storage key - visible and cleanable by an admin - rather than a row disappearing
// while its blob still silently occupies storage forever.
public sealed class DeleteSourceDocumentHandler(
    ISourceDocumentRepository sourceDocumentRepository, IFileStorage fileStorage)
    : IRequestHandler<DeleteSourceDocumentCommand, Result>
{
    public async Task<Result> Handle(DeleteSourceDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await sourceDocumentRepository.GetByIdAsync(request.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result.Failure(Error.NotFound("SourceDocument.NotFound", "No such document."));
        }

        await fileStorage.DeleteAsync(document.StorageKey, cancellationToken);
        sourceDocumentRepository.Remove(document);

        return Result.Success();
    }
}
