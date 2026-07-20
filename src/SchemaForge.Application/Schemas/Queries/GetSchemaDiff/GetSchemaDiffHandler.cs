using MediatR;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaDiff;

public sealed class GetSchemaDiffHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<GetSchemaDiffQuery, Result<SchemaDiff>>
{
    public async Task<Result<SchemaDiff>> Handle(GetSchemaDiffQuery request, CancellationToken cancellationToken)
    {
        // "against" is the baseline (before), the {id} in the route is the current version
        // (after) - so "added" means present in {id} but not in ?against=, matching how a caller
        // reads "diff this version against that one" (what's new/changed *since* the baseline).
        var current = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (current is null)
        {
            return Result<SchemaDiff>.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        var baseline = await schemaVersionRepository.GetByIdAsync(request.AgainstSchemaVersionId, cancellationToken);
        if (baseline is null)
        {
            return Result<SchemaDiff>.Failure(
                Error.NotFound("SchemaVersion.NotFound", "No such schema version to compare against."));
        }

        var diff = SchemaDiffComputer.Compute(
            baseline.RootNode, baseline.LocalDefinitions, current.RootNode, current.LocalDefinitions);

        return diff;
    }
}
