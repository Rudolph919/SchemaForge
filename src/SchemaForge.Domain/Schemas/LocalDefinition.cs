using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Schemas;

// Child of SchemaVersion (Step 4 §4.3) - within-version reuse, primarily for expressing
// recursion (a Category schema whose subcategories are shaped like Category itself). This is
// JSON Schema's $defs + local $ref in domain terms: scoped to one version, never independently
// versioned or shared across schemas (that's ComponentReference's job instead).
public sealed class LocalDefinition : Entity<Guid>
{
    public string Name { get; private set; } = null!;

    public SchemaNode RootNode { get; private set; } = null!;

    private LocalDefinition() { } // EF Core materialization

    private LocalDefinition(Guid id, string name, SchemaNode rootNode) : base(id)
    {
        Name = name;
        RootNode = rootNode;
    }

    internal static LocalDefinition Create(string name, NodeKind? rootKind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Local definition name is required.", nameof(name));
        }

        return new LocalDefinition(Guid.NewGuid(), name, SchemaNode.CreateEmpty(rootKind, null, 0));
    }
}
