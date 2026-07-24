using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.AddComponentLocalDefinition;

public sealed class AddComponentLocalDefinitionHandler(IComponentVersionRepository componentVersionRepository)
    : IRequestHandler<AddComponentLocalDefinitionCommand, Result<AddComponentLocalDefinitionResult>>
{
    public async Task<Result<AddComponentLocalDefinitionResult>> Handle(
        AddComponentLocalDefinitionCommand request, CancellationToken cancellationToken)
    {
        var version = await componentVersionRepository.GetByIdAsync(request.ComponentVersionId, cancellationToken);
        if (version is null)
        {
            return Result<AddComponentLocalDefinitionResult>.Failure(
                Error.NotFound("ComponentVersion.NotFound", "No such component version."));
        }

        var result = version.AddLocalDefinition(request.Name, request.RootKind);

        return result.IsFailure
            ? Result<AddComponentLocalDefinitionResult>.Failure(result.Error)
            : new AddComponentLocalDefinitionResult(result.Value);
    }
}
