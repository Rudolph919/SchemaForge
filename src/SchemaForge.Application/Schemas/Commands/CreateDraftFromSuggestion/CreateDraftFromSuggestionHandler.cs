using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas.Commands.CreateDraftFromSuggestion;

// Step 9 §2's critical property: this walks the accepted SuggestedNodes by calling the exact
// same SchemaVersion.AddObjectProperty/UpdateNode methods a human editing in the Designer would
// call (the same shape JsonSchemaImporter already established for Phase 4's import feature) - so
// every invariant the aggregate enforces on a human-authored node applies identically to an
// AI-suggested one. There is no code path where suggestion data becomes part of a SchemaVersion
// without passing through this same command a human-driven "accept" action uses.
public sealed class CreateDraftFromSuggestionHandler(
    ISchemaDefinitionRepository schemaDefinitionRepository,
    ISchemaVersionRepository schemaVersionRepository,
    ITenantContext tenantContext)
    : IRequestHandler<CreateDraftFromSuggestionCommand, Result<CreateDraftFromSuggestionResult>>
{
    public async Task<Result<CreateDraftFromSuggestionResult>> Handle(
        CreateDraftFromSuggestionCommand request, CancellationToken cancellationToken)
    {
        var schemaDefinition = await schemaDefinitionRepository.GetByIdAsync(request.SchemaDefinitionId, cancellationToken);
        if (schemaDefinition is null)
        {
            return Result<CreateDraftFromSuggestionResult>.Failure(
                Error.NotFound("SchemaDefinition.NotFound", "No such schema."));
        }

        // Same one-draft-at-a-time and version-bump rules as CreateSchemaVersionHandler/
        // ImportSchemaVersionHandler - a small amount of duplication accepted rather than forcing
        // a shared helper for what's otherwise a genuinely different command (Step 4's
        // ImportSchemaVersionHandler's own reasoning, restated here).
        if (await schemaVersionRepository.HasDraftAsync(request.SchemaDefinitionId, cancellationToken))
        {
            return Result<CreateDraftFromSuggestionResult>.Failure(Error.Conflict(
                "SchemaVersion.DraftAlreadyExists",
                "This schema already has a Draft version - publish or deprecate it before creating another."));
        }

        var latest = await schemaVersionRepository.GetLatestVersionNumberAsync(request.SchemaDefinitionId, cancellationToken);
        var nextVersion = latest is null ? SemVer.Initial : request.BumpKind switch
        {
            VersionBumpKind.Major => latest.NextMajor(),
            VersionBumpKind.Minor => latest.NextMinor(),
            VersionBumpKind.Patch => latest.NextPatch(),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.BumpKind, "Unknown version bump kind."),
        };

        var organizationId = tenantContext.CurrentTenantId!.Value;
        var version = SchemaVersion.CreateDraft(organizationId, request.SchemaDefinitionId, nextVersion, request.ChangeSummary);

        var acceptedIds = request.AcceptedNodeIds.ToHashSet();
        var acceptedCount = 0;
        foreach (var node in request.Suggestion.Nodes)
        {
            var result = AddAcceptedSubtree(version, version.RootNode.Id, node, acceptedIds, ref acceptedCount);
            if (result.IsFailure)
            {
                return Result<CreateDraftFromSuggestionResult>.Failure(result.Error);
            }
        }

        await schemaVersionRepository.AddAsync(version, cancellationToken);

        return new CreateDraftFromSuggestionResult(version.Id, nextVersion, acceptedCount);
    }

    // A rejected node's children are pruned along with it - there's no accepted parent left in
    // the resulting version for them to attach to.
    private static Result AddAcceptedSubtree(
        SchemaVersion version, Guid parentNodeId, SuggestedNode node, HashSet<Guid> acceptedIds, ref int acceptedCount)
    {
        if (!acceptedIds.Contains(node.Id))
        {
            return Result.Success();
        }

        var addResult = version.AddObjectProperty(parentNodeId, node.PropertyName ?? "field", node.Kind);
        if (addResult.IsFailure)
        {
            return Result.Failure(addResult.Error);
        }

        acceptedCount++;
        var newNodeId = addResult.Value;

        if (node.Description is not null)
        {
            var updateResult = version.UpdateNode(newNodeId, SchemaNodeContent.Empty(node.Kind) with { Description = node.Description });
            if (updateResult.IsFailure)
            {
                return updateResult;
            }
        }

        foreach (var child in node.Children)
        {
            var childResult = AddAcceptedSubtree(version, newNodeId, child, acceptedIds, ref acceptedCount);
            if (childResult.IsFailure)
            {
                return childResult;
            }
        }

        return Result.Success();
    }
}
