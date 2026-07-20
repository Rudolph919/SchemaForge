using SchemaForge.Domain.Schemas.ValueObjects;

namespace SchemaForge.Domain.Testing;

// Not an Entity<Guid> child of TestRun the way TestCase is a child of TestSuite - a TestRun's
// results are a bounded, immutable execution record written exactly once (Step 3 §3), so a plain
// value record serialized alongside TestRun's own jsonb column is enough; there's no case where
// one result needs to be looked up, mutated, or referenced independently of its TestRun.
public sealed record TestCaseResult(Guid TestCaseId, string TestCaseName, bool Passed, IReadOnlyList<ValidationError> ActualErrors);
