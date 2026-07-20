using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Queries.ListComponentVersions;

public sealed class ListComponentVersionsHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<ListComponentVersionsQuery, Result<IReadOnlyList<ComponentVersionSummary>>>
{
    public async Task<Result<IReadOnlyList<ComponentVersionSummary>>> Handle(
        ListComponentVersionsQuery request, CancellationToken cancellationToken)
    {
        var summaries = await componentVersionRepository.GetAllForComponentDefinitionAsync(
            request.ComponentDefinitionId, cancellationToken);

        return Result<IReadOnlyList<ComponentVersionSummary>>.Success(summaries);
    }
}
