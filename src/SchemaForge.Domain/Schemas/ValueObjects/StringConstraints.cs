namespace SchemaForge.Domain.Schemas.ValueObjects;

public sealed record StringConstraints(
    int? MinLength, int? MaxLength, string? Pattern, SchemaFormat? Format, string? CustomFormatValue);
