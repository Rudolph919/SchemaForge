namespace SchemaForge.Contracts.V1.Schemas;

public sealed record UpdateSchemaDefinitionDetailsRequest(string Name, string? Description, IReadOnlyList<string> Tags);
