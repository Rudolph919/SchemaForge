using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.UpdateComponentDefinitionDetails;

public sealed class UpdateComponentDefinitionDetailsHandler(IComponentDefinitionRepository componentDefinitionRepository)
    : IRequestHandler<UpdateComponentDefinitionDetailsCommand, Result>
{
    public async Task<Result> Handle(UpdateComponentDefinitionDetailsCommand request, CancellationToken cancellationToken)
    {
        var definition = await componentDefinitionRepository.GetByIdAsync(request.ComponentDefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure(Error.NotFound("ComponentDefinition.NotFound", "No such component."));
        }

        // Only checked when the name is actually changing - same guard as
        // UpdateSchemaDefinitionDetailsHandler, for the same reason (renaming to a component's
        // own current name must stay a harmless no-op, not a false conflict).
        if (!string.Equals(definition.Name, request.Name, StringComparison.Ordinal)
            && await componentDefinitionRepository.ExistsByNameAsync(definition.OrganizationId, request.Name, cancellationToken))
        {
            return Result.Failure(Error.Conflict(
                "ComponentDefinition.NameAlreadyExists", "A component with this name already exists in this organization."));
        }

        var renameResult = definition.Rename(request.Name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        definition.UpdateDescription(request.Description);

        return Result.Success();
    }
}
