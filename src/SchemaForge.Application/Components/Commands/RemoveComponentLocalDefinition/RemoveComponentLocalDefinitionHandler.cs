using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.RemoveComponentLocalDefinition;

public sealed class RemoveComponentLocalDefinitionHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<RemoveComponentLocalDefinitionCommand, Result>
{
    public async Task<Result> Handle(RemoveComponentLocalDefinitionCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        var result = version.RemoveLocalDefinition(request.LocalDefinitionId);
        if (result.IsSuccess)
        {
            componentVersionRepository.ApplyExpectedVersion(version, request.ExpectedVersion);
        }

        return result;
    }
}
