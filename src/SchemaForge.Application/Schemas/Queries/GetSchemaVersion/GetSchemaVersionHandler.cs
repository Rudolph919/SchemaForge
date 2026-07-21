using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaVersion;

public sealed class GetSchemaVersionHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<GetSchemaVersionQuery, Result<SchemaVersionDetail>>
{
    public async Task<Result<SchemaVersionDetail>> Handle(GetSchemaVersionQuery request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);

        if (version is null)
        {
            return Result<SchemaVersionDetail>.Failure(
                Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        return new SchemaVersionDetail(
            version.Id, version.SchemaDefinitionId, version.VersionNumber, version.Status,
            version.ChangeSummary, version.PublishedAt, version.RootNode, version.LocalDefinitions,
            version.RowVersion);
    }
}
