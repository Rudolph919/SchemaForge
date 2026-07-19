using SchemaForge.Domain.Schemas.Events;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Schemas;

// One aggregate instance per version ever created (Step 3 §2) - separate from SchemaDefinition
// specifically so an unbounded, ever-growing version history never has to load into memory for
// a metadata-only operation like renaming the schema. This is where the SchemaNode tree
// actually lives, and where immutability-after-publish is enforced at the method level (every
// mutating method below guards on Status == Draft), not just by convention.
public sealed class SchemaVersion : TenantOwnedAggregateRoot<Guid>
{
    public Guid SchemaDefinitionId { get; private set; }

    public SemVer VersionNumber { get; private set; } = null!;

    public SchemaLifecycleStatus Status { get; private set; }

    public string? ChangeSummary { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public SchemaNode RootNode { get; private set; } = null!;

    private List<LocalDefinition> _localDefinitions = [];
    public IReadOnlyList<LocalDefinition> LocalDefinitions => _localDefinitions;

    private SchemaVersion() { } // EF Core materialization

    private SchemaVersion(Guid id, Guid organizationId, Guid schemaDefinitionId, SemVer versionNumber, string? changeSummary)
        : base(id, organizationId)
    {
        SchemaDefinitionId = schemaDefinitionId;
        VersionNumber = versionNumber;
        ChangeSummary = changeSummary;
        Status = SchemaLifecycleStatus.Draft;
        RootNode = SchemaNode.CreateEmpty(NodeKind.Object, null, 0);
    }

    // Only-one-Draft-at-a-time and monotonic version numbering (Step 3 §4) are enforced at the
    // Application layer (a domain service queries existing versions first) and backed by a
    // Postgres partial unique index as the actual concurrency-safe guarantee - this factory
    // itself just builds a valid Draft from whatever version number it's given.
    public static SchemaVersion CreateDraft(
        Guid organizationId, Guid schemaDefinitionId, SemVer versionNumber, string? changeSummary = null)
    {
        var version = new SchemaVersion(Guid.NewGuid(), organizationId, schemaDefinitionId, versionNumber, changeSummary);
        version.RaiseDomainEvent(new SchemaVersionCreated(schemaDefinitionId, version.Id, versionNumber));

        return version;
    }

    public Result Publish()
    {
        if (Status != SchemaLifecycleStatus.Draft)
        {
            return Result.Failure(Error.Validation(
                "SchemaVersion.NotDraft", "Only a Draft version can be published."));
        }

        Status = SchemaLifecycleStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new SchemaVersionPublished(SchemaDefinitionId, Id));

        return Result.Success();
    }

    public Result Deprecate()
    {
        if (Status != SchemaLifecycleStatus.Published)
        {
            return Result.Failure(Error.Validation(
                "SchemaVersion.NotPublished", "Only a Published version can be deprecated."));
        }

        Status = SchemaLifecycleStatus.Deprecated;
        RaiseDomainEvent(new SchemaVersionDeprecated(SchemaDefinitionId, Id));

        return Result.Success();
    }

    public Result<Guid> AddObjectProperty(Guid parentNodeId, string propertyName, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return Result<Guid>.Failure(Error.Validation(
                "SchemaNode.PropertyNameRequired", "Property name is required."));
        }

        var parent = FindNode(parentNodeId);
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
        RaiseDomainEvent(new SchemaNodeAdded(Id, child.Id, propertyName));

        return child.Id;
    }

    public Result<Guid> AddArrayPrefixItem(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var parent = FindNode(parentNodeId);
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
        RaiseDomainEvent(new SchemaNodeAdded(Id, child.Id, null));

        return child.Id;
    }

    public Result<Guid> SetArrayItemsNode(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var parent = FindNode(parentNodeId);
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
        RaiseDomainEvent(new SchemaNodeAdded(Id, itemsNode.Id, null));

        return itemsNode.Id;
    }

    public Result<Guid> AddCompositionBranch(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var parent = FindNode(parentNodeId);
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
        RaiseDomainEvent(new SchemaNodeAdded(Id, branch.Id, null));

        return branch.Id;
    }

    public Result<Guid> SetConditionalNode(Guid parentNodeId, ConditionalSlot slot, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var parent = FindNode(parentNodeId);
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

        RaiseDomainEvent(new SchemaNodeAdded(Id, node.Id, null));

        return node.Id;
    }

    public Result UpdateNode(Guid nodeId, SchemaNodeContent content)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var node = FindNode(nodeId);
        if (node is null)
        {
            return Result.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        node.ApplyContent(content);
        RaiseDomainEvent(new SchemaNodeUpdated(Id, nodeId));

        return Result.Success();
    }

    public Result RemoveNode(Guid nodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        if (nodeId == RootNode.Id)
        {
            return Result.Failure(Error.Validation("SchemaNode.CannotRemoveRoot", "The root node cannot be removed."));
        }

        var removed = RootNode.TryRemoveDescendant(nodeId)
            || _localDefinitions.Any(d => d.RootNode.TryRemoveDescendant(nodeId));

        if (!removed)
        {
            return Result.Failure(Error.NotFound("SchemaNode.NotFound", "No such node."));
        }

        RaiseDomainEvent(new SchemaNodeRemoved(Id, nodeId));

        return Result.Success();
    }

    public Result<Guid> AddLocalDefinition(string name, NodeKind? rootKind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Guid>.Failure(Error.Validation(
                "LocalDefinition.NameRequired", "Local definition name is required."));
        }

        if (_localDefinitions.Any(d => d.Name == name))
        {
            return Result<Guid>.Failure(Error.Conflict(
                "LocalDefinition.DuplicateName", "A local definition with this name already exists."));
        }

        var definition = LocalDefinition.Create(name, rootKind);
        _localDefinitions.Add(definition);

        return definition.Id;
    }

    public Result RemoveLocalDefinition(Guid localDefinitionId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var removed = _localDefinitions.RemoveAll(d => d.Id == localDefinitionId) > 0;

        return removed
            ? Result.Success()
            : Result.Failure(Error.NotFound("LocalDefinition.NotFound", "No such local definition."));
    }

    private SchemaNode? FindNode(Guid nodeId) =>
        RootNode.FindDescendant(nodeId)
        ?? _localDefinitions.Select(d => d.RootNode.FindDescendant(nodeId)).FirstOrDefault(n => n is not null);

    private Result EnsureDraft() => Status == SchemaLifecycleStatus.Draft
        ? Result.Success()
        : Result.Failure(Error.Validation(
            "SchemaVersion.NotDraft", "Only a Draft version can be modified."));
}
