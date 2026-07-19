using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Workspaces;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.CreateSchemaDefinition;

public sealed class CreateSchemaDefinitionHandler(
    IProjectRepository projectRepository,
    ISchemaDefinitionRepository schemaDefinitionRepository,
    ITenantContext tenantContext)
    : IRequestHandler<CreateSchemaDefinitionCommand, Result<CreateSchemaDefinitionResult>>
{
    public async Task<Result<CreateSchemaDefinitionResult>> Handle(
        CreateSchemaDefinitionCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result<CreateSchemaDefinitionResult>.Failure(
                Error.NotFound("Project.NotFound", "No such project."));
        }

        if (await schemaDefinitionRepository.ExistsByNameAsync(request.ProjectId, request.Name, cancellationToken))
        {
            return Result<CreateSchemaDefinitionResult>.Failure(Error.Conflict(
                "SchemaDefinition.NameAlreadyExists", "A schema with this name already exists in this project."));
        }

        var organizationId = tenantContext.CurrentTenantId!.Value;
        var definition = SchemaDefinition.Create(organizationId, request.ProjectId, request.Name, request.Description);
        await schemaDefinitionRepository.AddAsync(definition, cancellationToken);

        return new CreateSchemaDefinitionResult(definition.Id);
    }
}
