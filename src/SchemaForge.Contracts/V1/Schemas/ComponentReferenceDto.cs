namespace SchemaForge.Contracts.V1.Schemas;

public sealed record ComponentReferenceDto(Guid ComponentVersionId, VersionConstraintDto Constraint);
