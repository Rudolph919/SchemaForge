using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.AddLocalDefinition;

public sealed class AddLocalDefinitionHandler(ISchemaVersionRepository schemaVersionRepository)
    : IRequestHandler<AddLocalDefinitionCommand, Result<AddLocalDefinitionResult>>
{
    public async Task<Result<AddLocalDefinitionResult>> Handle(
        AddLocalDefinitionCommand request, CancellationToken cancellationToken)
    {
        var version = await schemaVersionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
        {
            return Result<AddLocalDefinitionResult>.Failure(
                Error.NotFound("SchemaVersion.NotFound", "No such schema version."));
        }

        var result = version.AddLocalDefinition(request.Name, request.RootKind);

        return result.IsFailure
            ? Result<AddLocalDefinitionResult>.Failure(result.Error)
            : new AddLocalDefinitionResult(result.Value);
    }
}
