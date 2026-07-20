using SchemaForge.Application.Validation.Commands.ValidateJsonPayload;
using SchemaForge.Application.Validation.Queries.ListValidationRuns;
using SchemaForge.Contracts.V1.Validation;
using SchemaForge.Domain.Schemas.ValueObjects;
using DomainErrorSeverity = SchemaForge.Domain.Schemas.ErrorSeverity;
using DomainValidationOutcome = SchemaForge.Domain.Validation.ValidationOutcome;

namespace SchemaForge.Api.Mapping;

public static class ValidationMappingExtensions
{
    public static ValidateJsonPayloadResponse ToResponse(this ValidateJsonPayloadResult result) =>
        new(result.ValidationRunId, result.Outcome.ToContract(), [.. result.Errors.Select(e => e.ToResponse())]);

    public static ValidationRunSummaryResponse ToResponse(this ValidationRunSummary summary) => new(
        summary.Id, summary.Outcome.ToContract(), [.. summary.Errors.Select(e => e.ToResponse())],
        summary.ExecutedAt, summary.ExecutedByUserId);

    private static ValidationErrorResponse ToResponse(this ValidationError error) =>
        new(error.Path.Value, error.Code, error.Message, error.Severity.ToContract());

    private static ValidationOutcome ToContract(this DomainValidationOutcome outcome) => outcome switch
    {
        DomainValidationOutcome.Valid => ValidationOutcome.Valid,
        DomainValidationOutcome.Invalid => ValidationOutcome.Invalid,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown validation outcome."),
    };

    private static ErrorSeverity ToContract(this DomainErrorSeverity severity) => severity switch
    {
        DomainErrorSeverity.Error => ErrorSeverity.Error,
        DomainErrorSeverity.Warning => ErrorSeverity.Warning,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown error severity."),
    };
}
