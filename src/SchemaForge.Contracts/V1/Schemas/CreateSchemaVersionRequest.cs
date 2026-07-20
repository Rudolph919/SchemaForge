namespace SchemaForge.Contracts.V1.Schemas;

public sealed record CreateSchemaVersionRequest(VersionBumpKind BumpKind, string? ChangeSummary);
