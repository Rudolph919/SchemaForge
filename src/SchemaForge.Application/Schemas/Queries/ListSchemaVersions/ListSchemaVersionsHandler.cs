using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.ListSchemaVersions;

public sealed class ListSchemaVersionsHandler(ISchemaDefinitionRepository schemaDefinitionRepository, ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<ListSchemaVersionsQuery, Result<IReadOnlyList<SchemaVersionSummary>>>
{
    public async Task<Result<IReadOnlyList<SchemaVersionSummary>>> Handle(
        ListSchemaVersionsQuery request, CancellationToken cancellationToken)
    {
        var schemaDefinition = await schemaDefinitionRepository.GetByIdAsync(request.SchemaDefinitionId, cancellationToken);
        if (schemaDefinition is null)
        {
            return Result<IReadOnlyList<SchemaVersionSummary>>.Failure(
                Error.NotFound("SchemaDefinition.NotFound", "No such schema."));
        }

        var versions = await schemaVersionRepository.GetAllForSchemaDefinitionAsync(request.SchemaDefinitionId, cancellationToken);

        return Result<IReadOnlyList<SchemaVersionSummary>>.Success(versions);
    }
}
