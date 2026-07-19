using System.Runtime.CompilerServices;

// Infrastructure needs to reconstruct SchemaNode/LocalDefinition trees from their persisted
// JSON representation (Step 5 §2) via their internal Rehydrate factories - the same reason EF
// Core's own materializer needs access to every entity's private parameterless constructor,
// just for a case EF's own reflection-based materialization doesn't cover (a hand-rolled value
// converter, not model-based owned-type mapping).
[assembly: InternalsVisibleTo("SchemaForge.Infrastructure")]
