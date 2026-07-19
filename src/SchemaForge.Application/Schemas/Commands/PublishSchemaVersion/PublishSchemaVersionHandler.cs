using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.PublishSchemaVersion;

// Step 3 §4: publish must fail if any ComponentReference in the tree doesn't resolve to a
// Published ComponentVersion. Not implemented here - ComponentDefinition/ComponentVersion don't
// exist as queryable aggregates yet (that's Phase 3, per the roadmap); a SchemaNode can already
// carry a ComponentReference value object today (Step 4 §4.3), but nothing has a way to look one
// up. Revisit this handler when Phase 3 lands: resolve every ComponentVersionId referenced
// anywhere in the tree, verify each is Published, fail with a Conflict before calling
// version.Publish() if any doesn't resolve - same two-layer pattern as every other cross-
// aggregate invariant in this codebase.
public sealed class PublishSchemaVersionHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<PublishSchemaVersionCommand, Result>
{
    public async Task<Result> Handle(PublishSchemaVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        return version.Publish();
    }
}
