namespace SchemaForge.Contracts.V1.Validation;

public sealed record ValidationRunSummaryResponse(
    Guid Id, ValidationOutcome Outcome, IReadOnlyList<ValidationErrorResponse> Errors, DateTimeOffset ExecutedAt, Guid ExecutedByUserId);
