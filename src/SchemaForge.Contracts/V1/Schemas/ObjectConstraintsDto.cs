namespace SchemaForge.Contracts.V1.Schemas;

public sealed record ObjectConstraintsDto(int? MinProperties, int? MaxProperties, bool AdditionalPropertiesAllowed);
