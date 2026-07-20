namespace SchemaForge.Contracts.V1.Schemas;

public sealed record StringConstraintsDto(
    int? MinLength, int? MaxLength, string? Pattern, SchemaFormat? Format, string? CustomFormatValue);
