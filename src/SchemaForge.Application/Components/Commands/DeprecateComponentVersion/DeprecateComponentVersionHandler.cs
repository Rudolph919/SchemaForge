using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.DeprecateComponentVersion;

public sealed class DeprecateComponentVersionHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<DeprecateComponentVersionCommand, Result>
{
    public async Task<Result> Handle(DeprecateComponentVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        return version.Deprecate();
    }
}
