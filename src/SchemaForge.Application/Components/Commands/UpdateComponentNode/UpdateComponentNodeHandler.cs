using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.UpdateComponentNode;

public sealed class UpdateComponentNodeHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<UpdateComponentNodeCommand, Result>
{
    public async Task<Result> Handle(UpdateComponentNodeCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        return version.UpdateNode(request.NodeId, request.Content);
    }
}
