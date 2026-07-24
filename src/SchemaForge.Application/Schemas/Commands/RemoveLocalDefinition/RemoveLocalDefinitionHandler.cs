using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.RemoveLocalDefinition;

public sealed class RemoveLocalDefinitionHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<RemoveLocalDefinitionCommand, Result>
{
    public async Task<Result> Handle(RemoveLocalDefinitionCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        var result = version.RemoveLocalDefinition(request.LocalDefinitionId);
        if (result.IsSuccess)
        {
            schemaVersionRepository.ApplyExpectedVersion(version, request.ExpectedVersion);
        }

        return result;
    }
}
