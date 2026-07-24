using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas;

// Child entity of SchemaVersion (or ComponentVersion) - Step 3 §6, Step 4 §4.1. One base type
// with optional constraint bundles, not a subclass per JSON Schema keyword, because the spec
// itself allows keywords to co-occur on one schema object (e.g. type: object AND oneOf).
//
// No ParentNodeId: Step 4's illustrative field predates Step 5's JSONB decision. A row in a
// normalized self-referencing table needs a parent foreign key; a node nested inside its
// parent's own Properties/PrefixItems/ItemsNode/CompositionBranches/IfNode/ThenNode/ElseNode
// field doesn't - its position in the object graph already is its parent relationship. All
// mutation happens through SchemaVersion (Ground Rule 3), so every method here is internal.
public sealed class SchemaNode : Entity<Guid>
{
    public string? PropertyName { get; private set; }

    public int Order { get; private set; }

    public NodeKind? Kind { get; private set; }

    public string? Description { get; private set; }

    public string? Notes { get; private set; }

    public bool IsNullable { get; private set; }

    public bool IsRequiredByParent { get; private set; }

    private List<JsonLiteral> _examples = [];
    public IReadOnlyList<JsonLiteral> Examples => _examples;

    public JsonLiteral? DefaultValue { get; private set; }

    private List<JsonLiteral>? _allowedValues;
    public IReadOnlyList<JsonLiteral>? AllowedValues => _allowedValues;

    public JsonLiteral? ConstValue { get; private set; }

    public ObjectConstraints? ObjectConstraints { get; private set; }

    public ArrayConstraints? ArrayConstraints { get; private set; }

    public StringConstraints? StringConstraints { get; private set; }

    public NumericConstraints? NumericConstraints { get; private set; }

    private List<SchemaNode> _properties = [];
    public IReadOnlyList<SchemaNode> Properties => _properties;

    private List<SchemaNode> _prefixItems = [];
    public IReadOnlyList<SchemaNode> PrefixItems => _prefixItems;

    public SchemaNode? ItemsNode { get; private set; }

    private Dictionary<string, IReadOnlyList<string>>? _dependentRequired;
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? DependentRequired => _dependentRequired;

    public CompositionKind? Composition { get; private set; }

    private List<SchemaNode> _compositionBranches = [];
    public IReadOnlyList<SchemaNode> CompositionBranches => _compositionBranches;

    public SchemaNode? IfNode { get; private set; }

    public SchemaNode? ThenNode { get; private set; }

    public SchemaNode? ElseNode { get; private set; }

    public ComponentReference? ComponentReference { get; private set; }

    public Guid? LocalDefinitionRef { get; private set; }

    private SchemaNode() { } // EF Core materialization

    private SchemaNode(Guid id, NodeKind? kind, string? propertyName, int order) : base(id)
    {
        Kind = kind;
        PropertyName = propertyName;
        Order = order;
    }

    internal static SchemaNode CreateEmpty(NodeKind? kind, string? propertyName, int order) =>
        new(Guid.NewGuid(), kind, propertyName, order);

    // Used only by Infrastructure's JSON converter (Step 5 §2 - the RootNode/LocalDefinitions
    // tree is persisted as an opaque jsonb column via a value converter, not EF's native owned-
    // type mapping, which can't represent a genuinely self-referential recursive structure).
    // Reconstructing from storage needs the node's already-assigned Id, unlike CreateEmpty which
    // always mints a fresh one for a brand-new node.
    internal static SchemaNode Rehydrate(Guid id, NodeKind? kind, string? propertyName, int order) =>
        new(id, kind, propertyName, order);

    internal void ApplyContent(SchemaNodeContent content)
    {
        Kind = content.Kind;
        Description = content.Description;
        Notes = content.Notes;
        IsNullable = content.IsNullable;
        IsRequiredByParent = content.IsRequiredByParent;
        _examples = [.. content.Examples];
        DefaultValue = content.DefaultValue;
        _allowedValues = content.AllowedValues is null ? null : [.. content.AllowedValues];
        ConstValue = content.ConstValue;
        ObjectConstraints = content.ObjectConstraints;
        ArrayConstraints = content.ArrayConstraints;
        StringConstraints = content.StringConstraints;
        NumericConstraints = content.NumericConstraints;
        _dependentRequired = content.DependentRequired is null
            ? null
            : content.DependentRequired.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Composition = content.Composition;
        ComponentReference = content.ComponentReference;
        LocalDefinitionRef = content.LocalDefinitionRef;
    }

    internal void Reorder(int newOrder) => Order = newOrder;

    // Only meaningful when reparenting under a different attachment kind (Step 6 §2.4's
    // reparent extension) - a node moved into a non-ObjectProperty slot (array items, a
    // composition branch, a conditional slot) has no property name, the same as one created
    // fresh at that slot via AddArrayPrefixItem/SetArrayItemsNode/AddCompositionBranch/
    // SetConditionalNode all passing null.
    internal void Rename(string? propertyName) => PropertyName = propertyName;

    internal void AddProperty(SchemaNode child) => _properties.Add(child);

    internal bool RemoveProperty(Guid childId) => _properties.RemoveAll(n => n.Id == childId) > 0;

    internal void AddPrefixItem(SchemaNode child) => _prefixItems.Add(child);

    internal bool RemovePrefixItem(Guid childId) => _prefixItems.RemoveAll(n => n.Id == childId) > 0;

    internal void AddCompositionBranch(SchemaNode child) => _compositionBranches.Add(child);

    internal bool RemoveCompositionBranch(Guid childId) => _compositionBranches.RemoveAll(n => n.Id == childId) > 0;

    internal void SetItemsNode(SchemaNode? node) => ItemsNode = node;

    internal void SetIfNode(SchemaNode? node) => IfNode = node;

    internal void SetThenNode(SchemaNode? node) => ThenNode = node;

    internal void SetElseNode(SchemaNode? node) => ElseNode = node;

    // Depth-first search across every attachment point - self first so a caller can pass the
    // tree root without a special case.
    internal SchemaNode? FindDescendant(Guid id)
    {
        if (Id == id) return this;

        foreach (var child in _properties)
        {
            var found = child.FindDescendant(id);
            if (found is not null) return found;
        }

        foreach (var child in _prefixItems)
        {
            var found = child.FindDescendant(id);
            if (found is not null) return found;
        }

        if (ItemsNode?.FindDescendant(id) is { } inItems) return inItems;

        foreach (var branch in _compositionBranches)
        {
            var found = branch.FindDescendant(id);
            if (found is not null) return found;
        }

        if (IfNode?.FindDescendant(id) is { } inIf) return inIf;
        if (ThenNode?.FindDescendant(id) is { } inThen) return inThen;
        if (ElseNode?.FindDescendant(id) is { } inElse) return inElse;

        return null;
    }

    // Removes childId if it's attached directly under this node, at any attachment point
    // (list-based or single-slot). Does not recurse - TryRemoveDescendant is the recursive
    // caller-facing version.
    private bool TryRemoveDirectChild(Guid childId)
    {
        if (_properties.RemoveAll(n => n.Id == childId) > 0) return true;
        if (_prefixItems.RemoveAll(n => n.Id == childId) > 0) return true;
        if (_compositionBranches.RemoveAll(n => n.Id == childId) > 0) return true;
        if (ItemsNode?.Id == childId) { ItemsNode = null; return true; }
        if (IfNode?.Id == childId) { IfNode = null; return true; }
        if (ThenNode?.Id == childId) { ThenNode = null; return true; }
        if (ElseNode?.Id == childId) { ElseNode = null; return true; }
        return false;
    }

    // Depth-first: tries every attachment point on this node first, then recurses into each
    // child. Used by RemoveNode, which doesn't need to know in advance which attachment point a
    // node lives at - only that it's somewhere in the tree.
    internal bool TryRemoveDescendant(Guid childId)
    {
        if (TryRemoveDirectChild(childId)) return true;

        foreach (var child in _properties.Concat(_prefixItems).Concat(_compositionBranches))
        {
            if (child.TryRemoveDescendant(childId)) return true;
        }

        if (ItemsNode?.TryRemoveDescendant(childId) == true) return true;
        if (IfNode?.TryRemoveDescendant(childId) == true) return true;
        if (ThenNode?.TryRemoveDescendant(childId) == true) return true;
        if (ElseNode?.TryRemoveDescendant(childId) == true) return true;

        return false;
    }

    // Same walk as TryRemoveDirectChild, but returns the detached node instead of discarding it -
    // reparenting (Step 6 §2.4's reparent extension) needs the actual instance back so it can be
    // re-attached elsewhere with its id/content untouched, not just confirmation it was removed.
    private SchemaNode? TryDetachDirectChild(Guid childId)
    {
        var property = _properties.FirstOrDefault(n => n.Id == childId);
        if (property is not null) { _properties.Remove(property); return property; }

        var prefixItem = _prefixItems.FirstOrDefault(n => n.Id == childId);
        if (prefixItem is not null) { _prefixItems.Remove(prefixItem); return prefixItem; }

        var branch = _compositionBranches.FirstOrDefault(n => n.Id == childId);
        if (branch is not null) { _compositionBranches.Remove(branch); return branch; }

        if (ItemsNode?.Id == childId) { var node = ItemsNode; ItemsNode = null; return node; }
        if (IfNode?.Id == childId) { var node = IfNode; IfNode = null; return node; }
        if (ThenNode?.Id == childId) { var node = ThenNode; ThenNode = null; return node; }
        if (ElseNode?.Id == childId) { var node = ElseNode; ElseNode = null; return node; }

        return null;
    }

    // Depth-first equivalent of TryRemoveDescendant - used by ReparentNode, which needs to pull
    // the node out of wherever it currently lives before reattaching it under its new parent.
    internal SchemaNode? TryDetachDescendant(Guid childId)
    {
        var direct = TryDetachDirectChild(childId);
        if (direct is not null) return direct;

        foreach (var child in _properties.Concat(_prefixItems).Concat(_compositionBranches))
        {
            var found = child.TryDetachDescendant(childId);
            if (found is not null) return found;
        }

        if (ItemsNode?.TryDetachDescendant(childId) is { } inItems) return inItems;
        if (IfNode?.TryDetachDescendant(childId) is { } inIf) return inIf;
        if (ThenNode?.TryDetachDescendant(childId) is { } inThen) return inThen;
        if (ElseNode?.TryDetachDescendant(childId) is { } inElse) return inElse;

        return null;
    }
}
