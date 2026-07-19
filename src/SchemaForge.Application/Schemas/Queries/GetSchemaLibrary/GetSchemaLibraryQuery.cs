using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaLibrary;

// Unpaginated for now, matching ListProjectsQuery - a Project's schema count is realistically
// dozens, not the unbounded-over-time shape (Step 6 §1.3) that actually needs cursor pagination
// (audit_log_entries, validation_runs). Revisit if real usage proves that assumption wrong.
public sealed record GetSchemaLibraryQuery(Guid ProjectId) : IQuery<Result<IReadOnlyList<SchemaDefinitionSummary>>>;

public sealed record SchemaDefinitionSummary(Guid Id, string Name, string? Description, IReadOnlyList<string> Tags);
