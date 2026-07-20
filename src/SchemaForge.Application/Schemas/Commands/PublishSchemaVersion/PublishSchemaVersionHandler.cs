using MediatR;
using SchemaForge.Application.Common;
using SchemaForge.Application.Components;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.PublishSchemaVersion;

// Step 3 §4: publish fails if any ComponentReference anywhere in the tree doesn't resolve to a
// Published ComponentVersion - now that Phase 3 gives ComponentVersion a queryable repository,
// ComponentReferenceValidation.EnsureAllReferencesArePublishedAsync (shared with
// PublishComponentVersionHandler, since a ComponentVersion's own tree can reference other
// components too) does the actual walk-and-check.
public sealed class PublishSchemaVersionHandler(
    ISchemaVersionRepository schemaVersionRepository, IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<PublishSchemaVersionCommand, Result>
{
    public async Task<Result> Handle(PublishSchemaVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
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
