using System.Text.Json;
using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas.ValueObjects;
using SchemaForge.Domain.Validation;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Validation.Commands.ValidateJsonPayload;

// Modeled as ICommand, not IQuery, despite Step 6 §1.4 describing this endpoint as "Qry-shaped"
// - that description is about REST semantics (200 OK regardless of valid/invalid outcome, no
// created-resource response), not the MediatR pipeline. This handler has a real persistence side
// effect (a ValidationRun row), and TransactionBehavior - the only thing that ever calls
// SaveChangesAsync - is wired to ICommand specifically (Step 1 §3's own pipeline). An IQuery
// here would silently never persist anything.
public sealed record ValidateJsonPayloadCommand(Guid SchemaVersionId, JsonElement Payload)
    : ICommand<Result<ValidateJsonPayloadResult>>;

public sealed record ValidateJsonPayloadResult(
    Guid ValidationRunId, ValidationOutcome Outcome, IReadOnlyList<ValidationError> Errors);
