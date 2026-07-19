using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.DeprecateSchemaVersion;

public sealed class DeprecateSchemaVersionHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<DeprecateSchemaVersionCommand, Result>
{
    public async Task<Result> Handle(DeprecateSchemaVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        return version.Deprecate();
    }
}
