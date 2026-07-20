using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Queries.GetComponentDefinition;

public sealed class GetComponentDefinitionHandler(IComponentDefinitionRepository componentDefinitionRepository)
    : IRequestHandler<GetComponentDefinitionQuery, Result<ComponentDefinitionDetail>>
{
    public async Task<Result<ComponentDefinitionDetail>> Handle(
        GetComponentDefinitionQuery request, CancellationToken cancellationToken)
    {
        var definition = await componentDefinitionRepository.GetByIdAsync(request.ComponentDefinitionId, cancellationToken);

        if (definition is null)
        {
            return Result<ComponentDefinitionDetail>.Failure(Error.NotFound("ComponentDefinition.NotFound", "No such component."));
        }

        return new ComponentDefinitionDetail(definition.Id, definition.OrganizationId, definition.Name, definition.Description);
    }
}
