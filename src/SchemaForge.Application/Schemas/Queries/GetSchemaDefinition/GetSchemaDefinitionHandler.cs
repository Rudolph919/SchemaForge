using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaDefinition;

public sealed class GetSchemaDefinitionHandler(ISchemaDefinitionRepository schemaDefinitionRepository)
    : IRequestHandler<GetSchemaDefinitionQuery, Result<SchemaDefinitionDetail>>
{
    public async Task<Result<SchemaDefinitionDetail>> Handle(
        GetSchemaDefinitionQuery request, CancellationToken cancellationToken)
    {
        var definition = await schemaDefinitionRepository.GetByIdAsync(request.SchemaDefinitionId, cancellationToken);

        if (definition is null)
        {
            return Result<SchemaDefinitionDetail>.Failure(Error.NotFound("SchemaDefinition.NotFound", "No such schema."));
        }

        return new SchemaDefinitionDetail(
            definition.Id, definition.ProjectId, definition.Name, definition.Description, definition.Tags,
            definition.RowVersion);
    }
}
