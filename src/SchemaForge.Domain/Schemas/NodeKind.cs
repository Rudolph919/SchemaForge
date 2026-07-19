namespace SchemaForge.Domain.Schemas;

// Nullable at every use site (Step 4 §4.1) - a node can have no Kind at all when it's
// composition-only (e.g. a oneOf of otherwise-unrelated shapes with no properties of its own).
public enum NodeKind
{
    Object,
    Array,
    String,
    Number,
    Integer,
    Boolean,
    Null
}
