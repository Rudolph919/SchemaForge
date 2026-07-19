namespace SchemaForge.Domain.Schemas.ValueObjects;

public sealed record ArrayConstraints(int? MinItems, int? MaxItems, bool UniqueItems);
