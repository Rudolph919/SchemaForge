using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.MoveComponentNode;

public sealed class MoveComponentNodeHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<MoveComponentNodeCommand, Result>
{
    public async Task<Result> Handle(MoveComponentNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        return version.MoveNode(request.NodeId, request.NewOrder);
    }
}
