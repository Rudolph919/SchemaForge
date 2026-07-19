namespace SchemaForge.Domain.Schemas.ValueObjects;

// Points at an Organization-scoped ComponentVersion by ID only (Ground Rule 1) - cross-schema
// reuse (Step 2 §3). Version validity (does ComponentVersionId actually exist, does it resolve
// to a Published version when required) is checked at the Application layer, never here -
// Domain has no persistence access to verify it (Step 3 §4).
public sealed record ComponentReference(Guid ComponentVersionId, VersionConstraint Constraint);
