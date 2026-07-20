using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Queries.GetComponentLibrary;

public sealed class GetComponentLibraryHandler(
    IComponentDefinitionRepository componentDefinitionRepository, ITenantContext tenantContext)
    : IRequestHandler<GetComponentLibraryQuery, Result<IReadOnlyList<ComponentDefinitionSummary>>>
{
    public async Task<Result<IReadOnlyList<ComponentDefinitionSummary>>> Handle(
        GetComponentLibraryQuery request, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.CurrentTenantId!.Value;
        var definitions = await componentDefinitionRepository.GetAllForOrganizationAsync(organizationId, cancellationToken);

        var summaries = definitions
            .Select(d => new ComponentDefinitionSummary(d.Id, d.Name, d.Description))
            .ToList();

        return Result<IReadOnlyList<ComponentDefinitionSummary>>.Success(summaries);
    }
}
