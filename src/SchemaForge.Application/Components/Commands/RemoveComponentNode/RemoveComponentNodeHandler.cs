using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.RemoveComponentNode;

public sealed class RemoveComponentNodeHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<RemoveComponentNodeCommand, Result>
{
    public async Task<Result> Handle(RemoveComponentNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        var result = version.RemoveNode(request.NodeId);
        if (result.IsSuccess)
        {
            componentVersionRepository.ApplyExpectedVersion(version, request.ExpectedVersion);
        }

        return result;
    }
}
