using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.Domain.Validation;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Validation.Queries.ListValidationRuns;

// Unpaginated for now, same deliberate simplification as GetSchemaLibrary - validation_runs is
// exactly the kind of unbounded-over-time table Step 6 §1.3 says genuinely needs cursor
// pagination eventually, but that's shared infrastructure worth building once or its own PR, not
// bolted onto this endpoint ad hoc.
public sealed record ListValidationRunsQuery(Guid SchemaVersionId) : IQuery<Result<IReadOnlyList<ValidationRunSummary>>>;

public sealed record ValidationRunSummary(
    Guid Id, ValidationOutcome Outcome, IReadOnlyList<ValidationError> Errors, DateTimeOffset ExecutedAt, Guid ExecutedByUserId);
