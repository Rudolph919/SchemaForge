using MediatR;
using SchemaForge.Application.Workspaces;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaLibrary;

public sealed class GetSchemaLibraryHandler(
    IProjectRepository projectRepository, ISchemaDefinitionRepository schemaDefinitionRepository)
    : IRequestHandler<GetSchemaLibraryQuery, Result<IReadOnlyList<SchemaDefinitionSummary>>>
{
    public async Task<Result<IReadOnlyList<SchemaDefinitionSummary>>> Handle(
        GetSchemaLibraryQuery request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result<IReadOnlyList<SchemaDefinitionSummary>>.Failure(
                Error.NotFound("Project.NotFound", "No such project."));
        }

        var definitions = await schemaDefinitionRepository.GetAllForProjectAsync(request.ProjectId, cancellationToken);

        var summaries = definitions
            .Select(d => new SchemaDefinitionSummary(d.Id, d.Name, d.Description, d.Tags))
            .ToList();

        return Result<IReadOnlyList<SchemaDefinitionSummary>>.Success(summaries);
    }
}
