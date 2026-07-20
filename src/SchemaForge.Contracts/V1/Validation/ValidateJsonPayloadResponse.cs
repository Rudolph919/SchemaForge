namespace SchemaForge.Contracts.V1.Validation;

public sealed record ValidateJsonPayloadResponse(
    Guid ValidationRunId, ValidationOutcome Outcome, IReadOnlyList<ValidationErrorResponse> Errors);
