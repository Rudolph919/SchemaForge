using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas;

// The named, logical identity that persists across versions (Step 2 §1) - deliberately small and
// cheap to load (Step 3 §2). The actual schema structure lives in SchemaVersion, a separate
// aggregate root, specifically so renaming a schema never has to touch its (potentially large,
// ever-growing) version history.
public sealed class SchemaDefinition : TenantOwnedAggregateRoot<Guid>, IHasRowVersion
{
    public Guid ProjectId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public uint RowVersion { get; private set; }

    private List<string> _tags = [];
    public IReadOnlyList<string> Tags => _tags;

    private SchemaDefinition() { } // EF Core materialization

    private SchemaDefinition(Guid id, Guid organizationId, Guid projectId, string name, string? description)
        : base(id, organizationId)
    {
        ProjectId = projectId;
        Name = name;
        Description = description;
    }

    public static SchemaDefinition Create(
        Guid organizationId, Guid projectId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Schema name is required.", nameof(name));
        }

        var definition = new SchemaDefinition(Guid.NewGuid(), organizationId, projectId, name, description);
        definition.RaiseDomainEvent(new SchemaDefinitionCreated(organizationId, projectId, definition.Id, name));

        return definition;
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(Error.Validation("SchemaDefinition.NameRequired", "Schema name is required."));
        }

        Name = newName;
        return Result.Success();
    }

    public void UpdateDescription(string? description) => Description = description;

    public void UpdateTags(IReadOnlyList<string> tags) => _tags = [.. tags];
}
