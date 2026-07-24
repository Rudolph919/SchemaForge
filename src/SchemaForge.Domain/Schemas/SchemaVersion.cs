using SchemaForge.Domain.Schemas.Events;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Schemas;

// One aggregate instance per version ever created (Step 3 §2) - separate from SchemaDefinition
// specifically so an unbounded, ever-growing version history never has to load into memory for
// a metadata-only operation like renaming the schema. This is where the SchemaNode tree
// actually lives, and where immutability-after-publish is enforced at the method level (every
// mutating method below guards on Status == Draft), not just by convention.
public sealed class SchemaVersion : TenantOwnedAggregateRoot<Guid>, IHasRowVersion
{
    public Guid SchemaDefinitionId { get; private set; }

    public SemVer VersionNumber { get; private set; } = null!;

    public SchemaLifecycleStatus Status { get; private set; }

    public string? ChangeSummary { get; private set; }

    public uint RowVersion { get; private set; }

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

        var result = NodeTreeOperations.AddObjectProperty(RootNode, _localDefinitions, parentNodeId, propertyName, kind);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeAdded(Id, result.Value, propertyName));

        return result;
    }

    public Result<Guid> AddArrayPrefixItem(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.AddArrayPrefixItem(RootNode, _localDefinitions, parentNodeId, kind);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeAdded(Id, result.Value, null));

        return result;
    }

    public Result<Guid> SetArrayItemsNode(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.SetArrayItemsNode(RootNode, _localDefinitions, parentNodeId, kind);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeAdded(Id, result.Value, null));

        return result;
    }

    public Result<Guid> AddCompositionBranch(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.AddCompositionBranch(RootNode, _localDefinitions, parentNodeId, kind);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeAdded(Id, result.Value, null));

        return result;
    }

    public Result<Guid> SetConditionalNode(Guid parentNodeId, ConditionalSlot slot, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.SetConditionalNode(RootNode, _localDefinitions, parentNodeId, slot, kind);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeAdded(Id, result.Value, null));

        return result;
    }

    public Result UpdateNode(Guid nodeId, SchemaNodeContent content)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.UpdateNode(RootNode, _localDefinitions, nodeId, content);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeUpdated(Id, nodeId));

        return result;
    }

    // Reorder among existing siblings only (Step 6 §2.4's "move" endpoint) - ReparentNodeAs*
    // below is the counterpart for moving to a different parent.
    public Result MoveNode(Guid nodeId, int newOrder)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.MoveNode(RootNode, _localDefinitions, nodeId, newOrder);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsObjectProperty(Guid nodeId, Guid newParentNodeId, string propertyName)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsObjectProperty(RootNode, _localDefinitions, nodeId, newParentNodeId, propertyName);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsArrayPrefixItem(Guid nodeId, Guid newParentNodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsArrayPrefixItem(RootNode, _localDefinitions, nodeId, newParentNodeId);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsArrayItems(Guid nodeId, Guid newParentNodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsArrayItems(RootNode, _localDefinitions, nodeId, newParentNodeId);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsCompositionBranch(Guid nodeId, Guid newParentNodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsCompositionBranch(RootNode, _localDefinitions, nodeId, newParentNodeId);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsConditionalNode(Guid nodeId, Guid newParentNodeId, ConditionalSlot slot)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsConditionalNode(RootNode, _localDefinitions, nodeId, newParentNodeId, slot);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeUpdated(Id, nodeId));

        return result;
    }

    public Result RemoveNode(Guid nodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.RemoveNode(RootNode, _localDefinitions, nodeId);
        if (result.IsSuccess) RaiseDomainEvent(new SchemaNodeRemoved(Id, nodeId));

        return result;
    }

    public Result<Guid> AddLocalDefinition(string name, NodeKind? rootKind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.AddLocalDefinition(_localDefinitions, name, rootKind);
        if (result.IsSuccess) RaiseDomainEvent(new LocalDefinitionAdded(Id, result.Value, name));

        return result;
    }

    public Result RemoveLocalDefinition(Guid localDefinitionId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.RemoveLocalDefinition(_localDefinitions, localDefinitionId);
        if (result.IsSuccess) RaiseDomainEvent(new LocalDefinitionRemoved(Id, localDefinitionId));

        return result;
    }

    private Result EnsureDraft() => Status == SchemaLifecycleStatus.Draft
        ? Result.Success()
        : Result.Failure(Error.Validation(
            "SchemaVersion.NotDraft", "Only a Draft version can be modified."));
}
