using MediatR;
using SchemaForge.Application.Common;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.PublishComponentVersion;

// A ComponentVersion's own tree can reference other components (Step 4 §5's InvoiceLineItem-
// references-MoneyAmount example) - the same "every ComponentReference must resolve to Published"
// invariant that applies to SchemaVersion.Publish applies here too, for the same reason: nothing
// should ever end up Published while depending on something that isn't.
public sealed class PublishComponentVersionHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<PublishComponentVersionCommand, Result>
{
    public async Task<Result> Handle(PublishComponentVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        var referenceCheck = await ComponentReferenceValidation.EnsureAllReferencesArePublishedAsync(
            version.RootNode, version.LocalDefinitions, componentVersionRepository, cancellationToken);
        if (referenceCheck.IsFailure)
        {
            return referenceCheck;
        }

        return version.Publish();
    }
}
