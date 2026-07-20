using SchemaForge.Application.Components.Commands.CreateComponentDefinition;
using SchemaForge.Application.Components.Commands.UpdateComponentDefinitionDetails;
using SchemaForge.Application.Components.Queries.GetComponentDefinition;
using SchemaForge.Application.Components.Queries.GetComponentLibrary;
using SchemaForge.Contracts.V1.Components;

namespace SchemaForge.Api.Mapping;

public static class ComponentsMappingExtensions
{
    public static CreateComponentDefinitionCommand ToCommand(this CreateComponentDefinitionRequest request) =>
        new(request.Name, request.Description);

    public static CreateComponentDefinitionResponse ToResponse(this CreateComponentDefinitionResult result) =>
        new(result.ComponentDefinitionId);

    public static UpdateComponentDefinitionDetailsCommand ToCommand(
        this UpdateComponentDefinitionDetailsRequest request, Guid componentDefinitionId) =>
        new(componentDefinitionId, request.Name, request.Description);

    public static ComponentDefinitionSummaryResponse ToResponse(this ComponentDefinitionSummary summary) =>
        new(summary.Id, summary.Name, summary.Description);

    public static ComponentDefinitionDetailResponse ToResponse(this ComponentDefinitionDetail detail) =>
        new(detail.Id, detail.OrganizationId, detail.Name, detail.Description);
}
