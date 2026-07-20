using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas;

// The actual node-tree manipulation logic, shared between SchemaVersion and ComponentVersion
// (Step 7 §3: "both aggregates expose the same node-tree manipulation surface... implemented
// once against a shared abstraction, not duplicated per module"). Deliberately a stateless static
// helper operating on the tree primitives directly, not a shared base class - SchemaVersion and
// ComponentVersion are separate aggregate roots with their own draft-guards, domain events, and
// tenant/parent identity; only the tree-walking logic itself (find node, attach child, detach
// child, reorder) is actually identical between them.
internal static class NodeTreeOperations
{
    public static Result<Guid> AddObjectProperty(SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid parentNodeId, string propertyName, NodeKind? kind)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return Result<Guid>.Failure(Error.Validation(
                "SchemaNode.PropertyNameRequired", "Property name is required."));
        }

        var parent = FindNode(rootNode, localDefinitions, parentNodeId);
        if (parent is null)
        {
            return Result<Guid>.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        if (parent.Kind != NodeKind.Object)
        {
            return Result<Guid>.Failure(Error.Validation(
                "SchemaNode.NotAnObject", "Properties can only be added to an object node."));
        }

        if (parent.Properties.Any(p => p.PropertyName == propertyName))
        {
            return Result<Guid>.Failure(Error.Conflict(
                "SchemaNode.DuplicatePropertyName", "A property with this name already exists on this node."));
        }

        var child = SchemaNode.CreateEmpty(kind, propertyName, parent.Properties.Count);
        parent.AddProperty(child);

        return child.Id;
    }

    public static Result<Guid> AddArrayPrefixItem(
        SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid parentNodeId, NodeKind? kind)
    {
        var parent = FindNode(rootNode, localDefinitions, parentNodeId);
        if (parent is null)
        {
            return Result<Guid>.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        if (parent.Kind != NodeKind.Array)
        {
            return Result<Guid>.Failure(Error.Validation(
                "SchemaNode.NotAnArray", "Prefix items can only be added to an array node."));
        }

        var child = SchemaNode.CreateEmpty(kind, null, parent.PrefixItems.Count);
        parent.AddPrefixItem(child);

        return child.Id;
    }

    public static Result<Guid> SetArrayItemsNode(
        SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid parentNodeId, NodeKind? kind)
    {
        var parent = FindNode(rootNode, localDefinitions, parentNodeId);
        if (parent is null)
        {
            return Result<Guid>.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        if (parent.Kind != NodeKind.Array)
        {
            return Result<Guid>.Failure(Error.Validation(
                "SchemaNode.NotAnArray", "An items schema can only be set on an array node."));
        }

        var itemsNode = SchemaNode.CreateEmpty(kind, null, 0);
        parent.SetItemsNode(itemsNode);

        return itemsNode.Id;
    }

    public static Result<Guid> AddCompositionBranch(
        SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid parentNodeId, NodeKind? kind)
    {
        var parent = FindNode(rootNode, localDefinitions, parentNodeId);
        if (parent is null)
        {
            return Result<Guid>.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        if (parent.Composition is null)
        {
            return Result<Guid>.Failure(Error.Validation(
                "SchemaNode.NoComposition",
                "This node has no composition (oneOf/anyOf/allOf/not) set - set one before adding branches."));
        }

        var branch = SchemaNode.CreateEmpty(kind, null, parent.CompositionBranches.Count);
        parent.AddCompositionBranch(branch);

        return branch.Id;
    }

    public static Result<Guid> SetConditionalNode(
        SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid parentNodeId, ConditionalSlot slot, NodeKind? kind)
    {
        var parent = FindNode(rootNode, localDefinitions, parentNodeId);
        if (parent is null)
        {
            return Result<Guid>.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        var node = SchemaNode.CreateEmpty(kind, null, 0);
        switch (slot)
        {
            case ConditionalSlot.If: parent.SetIfNode(node); break;
            case ConditionalSlot.Then: parent.SetThenNode(node); break;
            case ConditionalSlot.Else: parent.SetElseNode(node); break;
            default: throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
        }

        return node.Id;
    }

    public static Result UpdateNode(
        SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid nodeId, SchemaNodeContent content)
    {
        var node = FindNode(rootNode, localDefinitions, nodeId);
        if (node is null)
        {
            return Result.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        node.ApplyContent(content);

        return Result.Success();
    }

    // Reorder among existing siblings only - not reparenting to a different node. Reparenting
    // would mean detaching from one attachment point and reattaching at another while preserving
    // the node's id/content, a materially riskier operation than resequencing a list; scoped out
    // until a real need for it shows up.
    public static Result MoveNode(SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid nodeId, int newOrder)
    {
        if (nodeId == rootNode.Id)
        {
            return Result.Failure(Error.Validation("SchemaNode.CannotMoveRoot", "The root node cannot be reordered."));
        }

        var node = FindNode(rootNode, localDefinitions, nodeId);
        if (node is null)
        {
            return Result.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        node.Reorder(newOrder);

        return Result.Success();
    }

    public static Result RemoveNode(SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid nodeId)
    {
        if (nodeId == rootNode.Id)
        {
            return Result.Failure(Error.Validation("SchemaNode.CannotRemoveRoot", "The root node cannot be removed."));
        }

        var removed = rootNode.TryRemoveDescendant(nodeId)
            || localDefinitions.Any(d => d.RootNode.TryRemoveDescendant(nodeId));

        return removed
            ? Result.Success()
            : Result.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
    }

    public static Result<Guid> AddLocalDefinition(List<LocalDefinition> localDefinitions, string name, NodeKind? rootKind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Guid>.Failure(Error.Validation(
                "LocalDefinition.NameRequired", "Local definition name is required."));
        }

        if (localDefinitions.Any(d => d.Name == name))
        {
            return Result<Guid>.Failure(Error.Conflict(
                "LocalDefinition.DuplicateName", "A local definition with this name already exists."));
        }

        var definition = LocalDefinition.Create(name, rootKind);
        localDefinitions.Add(definition);

        return definition.Id;
    }

    public static Result RemoveLocalDefinition(List<LocalDefinition> localDefinitions, Guid localDefinitionId)
    {
        var removed = localDefinitions.RemoveAll(d => d.Id == localDefinitionId) > 0;

        return removed
            ? Result.Success()
            : Result.Failure(Error.NotFound("LocalDefinition.NotFound", "No such local definition."));
    }

    private static SchemaNode? FindNode(SchemaNode rootNode, IReadOnlyList<LocalDefinition> localDefinitions, Guid nodeId) =>
        rootNode.FindDescendant(nodeId)
        ?? localDefinitions.Select(d => d.RootNode.FindDescendant(nodeId)).FirstOrDefault(n => n is not null);
}
