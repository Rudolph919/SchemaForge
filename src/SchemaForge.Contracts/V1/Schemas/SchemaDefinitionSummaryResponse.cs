namespace SchemaForge.Contracts.V1.Schemas;

public sealed record SchemaDefinitionSummaryResponse(
    Guid Id, string Name, string? Description, IReadOnlyList<string> Tags);
