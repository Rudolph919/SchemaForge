using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Components;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.CreateComponentDefinition;

public sealed class CreateComponentDefinitionHandler(
    IComponentDefinitionRepository componentDefinitionRepository, ITenantContext tenantContext)
    : IRequestHandler<CreateComponentDefinitionCommand, Result<CreateComponentDefinitionResult>>
{
    public async Task<Result<CreateComponentDefinitionResult>> Handle(
        CreateComponentDefinitionCommand request, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.CurrentTenantId!.Value;

        if (await componentDefinitionRepository.ExistsByNameAsync(organizationId, request.Name, cancellationToken))
        {
            return Result<CreateComponentDefinitionResult>.Failure(Error.Conflict(
                "ComponentDefinition.NameAlreadyExists", "A component with this name already exists in this organization."));
        }

        var definition = ComponentDefinition.Create(organizationId, request.Name, request.Description);
        await componentDefinitionRepository.AddAsync(definition, cancellationToken);

        return new CreateComponentDefinitionResult(definition.Id);
    }
}
