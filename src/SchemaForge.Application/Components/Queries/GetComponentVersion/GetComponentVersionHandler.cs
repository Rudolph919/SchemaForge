using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Queries.GetComponentVersion;

public sealed class GetComponentVersionHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<GetComponentVersionQuery, Result<ComponentVersionDetail>>
{
    public async Task<Result<ComponentVersionDetail>> Handle(GetComponentVersionQuery request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);

        if (version is null)
        {
            return Result<ComponentVersionDetail>.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        return new ComponentVersionDetail(
            version.Id, version.ComponentDefinitionId, version.VersionNumber, version.Status,
            version.ChangeSummary, version.PublishedAt, version.RootNode, version.LocalDefinitions,
            version.RowVersion);
    }
}
