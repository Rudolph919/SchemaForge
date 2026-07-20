using MediatR;
using SchemaForge.Application.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Validation.Queries.ListValidationRuns;

public sealed class ListValidationRunsHandler(
    ISchemaVersionRepository schemaVersionRepository, IValidationRunRepository validationRunRepository)
    : IRequestHandler<ListValidationRunsQuery, Result<IReadOnlyList<ValidationRunSummary>>>
{
    public async Task<Result<IReadOnlyList<ValidationRunSummary>>> Handle(
        ListValidationRunsQuery request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result<IReadOnlyList<ValidationRunSummary>>.Failure(
                Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        var runs = await validationRunRepository.GetAllForSchemaVersionAsync(request.SchemaVersionId, cancellationToken);

        var summaries = runs
            .Select(r => new ValidationRunSummary(r.Id, r.Outcome, r.Errors, r.ExecutedAt, r.ExecutedByUserId))
            .ToList();

        return Result<IReadOnlyList<ValidationRunSummary>>.Success(summaries);
    }
}
