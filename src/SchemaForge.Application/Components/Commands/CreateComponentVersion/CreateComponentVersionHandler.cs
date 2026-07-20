using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Schemas;
using SchemaForge.Domain.Components;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Components.Commands.CreateComponentVersion;

public sealed class CreateComponentVersionHandler(
    IComponentDefinitionRepository componentDefinitionRepository,
    IComponentVersionRepository componentVersionRepository,
    ITenantContext tenantContext)
    : IRequestHandler<CreateComponentVersionCommand, Result<CreateComponentVersionResult>>
{
    public async Task<Result<CreateComponentVersionResult>> Handle(
        CreateComponentVersionCommand request, CancellationToken cancellationToken)
    {
        var componentDefinition = await componentDefinitionRepository.GetByIdAsync(request.ComponentDefinitionId, cancellationToken);
        if (componentDefinition is null)
        {
            return Result<CreateComponentVersionResult>.Failure(
                Error.NotFound("ComponentDefinition.NotFound", "No such component."));
        }

        if (await componentVersionRepository.HasDraftAsync(request.ComponentDefinitionId, cancellationToken))
        {
            return Result<CreateComponentVersionResult>.Failure(Error.Conflict(
                "ComponentVersion.DraftAlreadyExists",
                "This component already has a Draft version - publish or deprecate it before creating another."));
        }

        var latest = await componentVersionRepository.GetLatestVersionNumberAsync(request.ComponentDefinitionId, cancellationToken);
        var nextVersion = latest is null ? SemVer.Initial : request.BumpKind switch
        {
            VersionBumpKind.Major => latest.NextMajor(),
            VersionBumpKind.Minor => latest.NextMinor(),
            VersionBumpKind.Patch => latest.NextPatch(),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.BumpKind, "Unknown version bump kind."),
        };

        var organizationId = tenantContext.CurrentTenantId!.Value;
        var version = ComponentVersion.CreateDraft(organizationId, request.ComponentDefinitionId, nextVersion, request.ChangeSummary);
        await componentVersionRepository.AddAsync(version, cancellationToken);

        return new CreateComponentVersionResult(version.Id, nextVersion);
    }
}
