using SchemaForge.Application.Schemas.Commands.CreateSchemaDefinition;
using SchemaForge.Application.Schemas.Commands.UpdateSchemaDefinitionDetails;
using SchemaForge.Application.Schemas.Queries.GetSchemaDefinition;
using SchemaForge.Application.Schemas.Queries.GetSchemaLibrary;
using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Api.Mapping;

public static class SchemasMappingExtensions
{
    public static CreateSchemaDefinitionCommand ToCommand(this CreateSchemaDefinitionRequest request, Guid projectId) =>
        new(projectId, request.Name, request.Description);

    public static CreateSchemaDefinitionResponse ToResponse(this CreateSchemaDefinitionResult result) =>
        new(result.SchemaDefinitionId);

    public static UpdateSchemaDefinitionDetailsCommand ToCommand(
        this UpdateSchemaDefinitionDetailsRequest request, Guid schemaDefinitionId, uint expectedVersion) =>
        new(schemaDefinitionId, request.Name, request.Description, request.Tags, expectedVersion);

    public static SchemaDefinitionSummaryResponse ToResponse(this SchemaDefinitionSummary summary) =>
        new(summary.Id, summary.Name, summary.Description, summary.Tags);

    public static SchemaDefinitionDetailResponse ToResponse(this SchemaDefinitionDetail detail) =>
        new(detail.Id, detail.ProjectId, detail.Name, detail.Description, detail.Tags);
}
