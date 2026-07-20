using SchemaForge.Domain.Components.Events;
using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Components;

// The named, logical identity that persists across versions - same split rationale as
// SchemaDefinition/SchemaVersion (Step 3 §2). Organization-scoped, not Project-scoped: a
// reusable component (e.g. "PostalAddress") is meant to be shared across every schema in the
// org, not owned by whichever project happened to define it first.
public sealed class ComponentDefinition : TenantOwnedAggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    private ComponentDefinition() { } // EF Core materialization

    private ComponentDefinition(Guid id, Guid organizationId, string name, string? description)
        : base(id, organizationId)
    {
        Name = name;
        Description = description;
    }

    public static ComponentDefinition Create(Guid organizationId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Component name is required.", nameof(name));
        }

        var definition = new ComponentDefinition(Guid.NewGuid(), organizationId, name, description);
        definition.RaiseDomainEvent(new ComponentDefinitionCreated(organizationId, definition.Id, name));

        return definition;
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(Error.Validation("ComponentDefinition.NameRequired", "Component name is required."));
        }

        Name = newName;
        return Result.Success();
    }

    public void UpdateDescription(string? description) => Description = description;
}
