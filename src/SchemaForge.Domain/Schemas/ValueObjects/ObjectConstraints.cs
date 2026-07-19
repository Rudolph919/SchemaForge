namespace SchemaForge.Domain.Schemas.ValueObjects;

public sealed record ObjectConstraints(int? MinProperties, int? MaxProperties, bool AdditionalPropertiesAllowed);
