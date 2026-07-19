namespace SchemaForge.Domain.Schemas.ValueObjects;

// Shared by NodeKind.Number and NodeKind.Integer (Step 4 §2).
public sealed record NumericConstraints(
    decimal? Minimum, decimal? Maximum, bool ExclusiveMinimum, bool ExclusiveMaximum, decimal? MultipleOf);
