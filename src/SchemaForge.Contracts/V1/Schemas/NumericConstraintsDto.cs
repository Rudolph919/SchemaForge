namespace SchemaForge.Contracts.V1.Schemas;

public sealed record NumericConstraintsDto(
    decimal? Minimum, decimal? Maximum, bool ExclusiveMinimum, bool ExclusiveMaximum, decimal? MultipleOf);
