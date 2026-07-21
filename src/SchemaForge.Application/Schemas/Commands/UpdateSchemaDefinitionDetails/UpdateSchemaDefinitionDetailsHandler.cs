using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.UpdateSchemaDefinitionDetails;

public sealed class UpdateSchemaDefinitionDetailsHandler(ISchemaDefinitionRepository schemaDefinitionRepository)
    : IRequestHandler<UpdateSchemaDefinitionDetailsCommand, Result>
{
    public async Task<Result> Handle(UpdateSchemaDefinitionDetailsCommand request, CancellationToken cancellationToken)
    {
        var definition = await schemaDefinitionRepository.GetByIdAsync(request.SchemaDefinitionId, cancellationToken);

        if (definition is null)
        {
            return Result.Failure(Error.NotFound("SchemaDefinition.NotFound", "No such schema."));
        }

        // Only checked when the name is actually changing - renaming to a schema's own current
        // name must stay a harmless no-op, not a false conflict. Without this, renaming into an
        // already-taken name fell through to the DB unique index and crashed with a raw 500
        // instead of a clean 409 - caught live during verification.
        if (!string.Equals(definition.Name, request.Name, StringComparison.Ordinal)
            && await schemaDefinitionRepository.ExistsByNameAsync(definition.ProjectId, request.Name, cancellationToken))
        {
            return Result.Failure(Error.Conflict(
                "SchemaDefinition.NameAlreadyExists", "A schema with this name already exists in this project."));
        }

        var renameResult = definition.Rename(request.Name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        definition.UpdateDescription(request.Description);
        definition.UpdateTags(request.Tags);
        schemaDefinitionRepository.ApplyExpectedVersion(definition, request.ExpectedVersion);

        return Result.Success();
    }
}
