using SchemaForge.Domain.Components.Events;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Components;

// Same shape as SchemaVersion, deliberately - Step 4 §5: "no new concepts." Reuses SchemaNode/
// LocalDefinition directly (a ComponentVersion's RootNode can itself hold ComponentReferences to
// other components, e.g. an InvoiceLineItem component referencing a MoneyAmount component) and
// the same NodeTreeOperations helper for all tree mutation, per Step 7 §3's shared-implementation
// note - only the draft-guard, domain events, and parent identity (ComponentDefinitionId instead
// of SchemaDefinitionId) actually differ from SchemaVersion.
public sealed class ComponentVersion : TenantOwnedAggregateRoot<Guid>, IHasRowVersion
{
    public Guid ComponentDefinitionId { get; private set; }

    public SemVer VersionNumber { get; private set; } = null!;

    public SchemaLifecycleStatus Status { get; private set; }

    public string? ChangeSummary { get; private set; }

    public uint RowVersion { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public SchemaNode RootNode { get; private set; } = null!;

    private List<LocalDefinition> _localDefinitions = [];
    public IReadOnlyList<LocalDefinition> LocalDefinitions => _localDefinitions;

    private ComponentVersion() { } // EF Core materialization

    private ComponentVersion(
        Guid id, Guid organizationId, Guid componentDefinitionId, SemVer versionNumber, string? changeSummary)
        : base(id, organizationId)
    {
        ComponentDefinitionId = componentDefinitionId;
        VersionNumber = versionNumber;
        ChangeSummary = changeSummary;
        Status = SchemaLifecycleStatus.Draft;
        RootNode = SchemaNode.CreateEmpty(NodeKind.Object, null, 0);
    }

    // Only-one-Draft-at-a-time and monotonic version numbering enforced the same way as
    // SchemaVersion (Application-layer check backed by a Postgres partial unique index).
    public static ComponentVersion CreateDraft(
        Guid organizationId, Guid componentDefinitionId, SemVer versionNumber, string? changeSummary = null)
    {
        var version = new ComponentVersion(Guid.NewGuid(), organizationId, componentDefinitionId, versionNumber, changeSummary);
        version.RaiseDomainEvent(new ComponentVersionCreated(componentDefinitionId, version.Id, versionNumber));

        return version;
    }

    public Result Publish()
    {
        if (Status != SchemaLifecycleStatus.Draft)
        {
            return Result.Failure(Error.Validation(
                "ComponentVersion.NotDraft", "Only a Draft version can be published."));
        }

        Status = SchemaLifecycleStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ComponentVersionPublished(ComponentDefinitionId, Id));

        return Result.Success();
    }

    public Result Deprecate()
    {
        if (Status != SchemaLifecycleStatus.Published)
        {
            return Result.Failure(Error.Validation(
                "ComponentVersion.NotPublished", "Only a Published version can be deprecated."));
        }

        Status = SchemaLifecycleStatus.Deprecated;
        RaiseDomainEvent(new ComponentVersionDeprecated(ComponentDefinitionId, Id));

        return Result.Success();
    }

    public Result<Guid> AddObjectProperty(Guid parentNodeId, string propertyName, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.AddObjectProperty(RootNode, _localDefinitions, parentNodeId, propertyName, kind);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeAdded(Id, result.Value, propertyName));

        return result;
    }

    public Result<Guid> AddArrayPrefixItem(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.AddArrayPrefixItem(RootNode, _localDefinitions, parentNodeId, kind);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeAdded(Id, result.Value, null));

        return result;
    }

    public Result<Guid> SetArrayItemsNode(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.SetArrayItemsNode(RootNode, _localDefinitions, parentNodeId, kind);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeAdded(Id, result.Value, null));

        return result;
    }

    public Result<Guid> AddCompositionBranch(Guid parentNodeId, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.AddCompositionBranch(RootNode, _localDefinitions, parentNodeId, kind);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeAdded(Id, result.Value, null));

        return result;
    }

    public Result<Guid> SetConditionalNode(Guid parentNodeId, ConditionalSlot slot, NodeKind? kind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.SetConditionalNode(RootNode, _localDefinitions, parentNodeId, slot, kind);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeAdded(Id, result.Value, null));

        return result;
    }

    public Result UpdateNode(Guid nodeId, SchemaNodeContent content)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.UpdateNode(RootNode, _localDefinitions, nodeId, content);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeUpdated(Id, nodeId));

        return result;
    }

    public Result MoveNode(Guid nodeId, int newOrder)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.MoveNode(RootNode, _localDefinitions, nodeId, newOrder);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsObjectProperty(Guid nodeId, Guid newParentNodeId, string propertyName)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsObjectProperty(RootNode, _localDefinitions, nodeId, newParentNodeId, propertyName);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsArrayPrefixItem(Guid nodeId, Guid newParentNodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsArrayPrefixItem(RootNode, _localDefinitions, nodeId, newParentNodeId);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsArrayItems(Guid nodeId, Guid newParentNodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsArrayItems(RootNode, _localDefinitions, nodeId, newParentNodeId);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsCompositionBranch(Guid nodeId, Guid newParentNodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsCompositionBranch(RootNode, _localDefinitions, nodeId, newParentNodeId);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeUpdated(Id, nodeId));

        return result;
    }

    public Result ReparentNodeAsConditionalNode(Guid nodeId, Guid newParentNodeId, ConditionalSlot slot)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.ReparentAsConditionalNode(RootNode, _localDefinitions, nodeId, newParentNodeId, slot);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeUpdated(Id, nodeId));

        return result;
    }

    public Result RemoveNode(Guid nodeId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.RemoveNode(RootNode, _localDefinitions, nodeId);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentNodeRemoved(Id, nodeId));

        return result;
    }

    public Result<Guid> AddLocalDefinition(string name, NodeKind? rootKind)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return Result<Guid>.Failure(draftCheck.Error);

        var result = NodeTreeOperations.AddLocalDefinition(_localDefinitions, name, rootKind);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentLocalDefinitionAdded(Id, result.Value, name));

        return result;
    }

    public Result RemoveLocalDefinition(Guid localDefinitionId)
    {
        var draftCheck = EnsureDraft();
        if (draftCheck.IsFailure) return draftCheck;

        var result = NodeTreeOperations.RemoveLocalDefinition(_localDefinitions, localDefinitionId);
        if (result.IsSuccess) RaiseDomainEvent(new ComponentLocalDefinitionRemoved(Id, localDefinitionId));

        return result;
    }

    private Result EnsureDraft() => Status == SchemaLifecycleStatus.Draft
        ? Result.Success()
        : Result.Failure(Error.Validation(
            "ComponentVersion.NotDraft", "Only a Draft version can be modified."));
}
