namespace SchemaForge.Contracts.V1.Schemas;

public sealed record VersionConstraintDto(VersionConstraintKind Kind, string? Version);
