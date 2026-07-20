using SchemaForge.Application.Components;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Common;

// Step 3 §4's cross-aggregate invariant: a version can only be published if every
// ComponentReference anywhere in its tree resolves to a Published ComponentVersion. Shared
// between PublishSchemaVersionHandler and PublishComponentVersionHandler - a ComponentVersion's
// own tree can reference other components too (Step 4 §5's InvoiceLineItem-references-MoneyAmount
// example), so the same check applies symmetrically to both. Lives in Application, not Domain,
// because it needs IComponentVersionRepository - a cross-aggregate lookup no single aggregate can
// perform on itself.
internal static class ComponentReferenceValidation
{
    public static async Task<Result> EnsureAllReferencesArePublishedAsync(
        SchemaNode rootNode,
        IReadOnlyList<LocalDefinition> localDefinitions,
        IComponentVersionRepository componentVersionRepository,
        CancellationToken cancellationToken)
    {
        var referencedIds = new HashSet<Guid>();
        CollectReferences(rootNode, referencedIds);
        foreach (var localDefinition in localDefinitions)
        {
            CollectReferences(localDefinition.RootNode, referencedIds);
        }

        foreach (var componentVersionId in referencedIds)
        {
            var componentVersion = await componentVersionRepository.GetByIdAsync(componentVersionId, cancellationToken);
            if (componentVersion is null || componentVersion.Status != SchemaLifecycleStatus.Published)
            {
                return Result.Failure(Error.Conflict(
                    "ComponentReference.NotPublished",
                    "Every referenced component must be Published before this version can be published."));
            }
        }

        return Result.Success();
    }

    private static void CollectReferences(SchemaNode node, HashSet<Guid> referencedIds)
    {
        if (node.ComponentReference is not null)
        {
            referencedIds.Add(node.ComponentReference.ComponentVersionId);
        }

        foreach (var child in node.Properties) CollectReferences(child, referencedIds);
        foreach (var child in node.PrefixItems) CollectReferences(child, referencedIds);
        foreach (var child in node.CompositionBranches) CollectReferences(child, referencedIds);
        if (node.ItemsNode is not null) CollectReferences(node.ItemsNode, referencedIds);
        if (node.IfNode is not null) CollectReferences(node.IfNode, referencedIds);
        if (node.ThenNode is not null) CollectReferences(node.ThenNode, referencedIds);
        if (node.ElseNode is not null) CollectReferences(node.ElseNode, referencedIds);
    }
}
