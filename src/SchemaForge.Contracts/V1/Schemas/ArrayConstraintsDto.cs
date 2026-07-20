namespace SchemaForge.Contracts.V1.Schemas;

public sealed record ArrayConstraintsDto(int? MinItems, int? MaxItems, bool UniqueItems);
